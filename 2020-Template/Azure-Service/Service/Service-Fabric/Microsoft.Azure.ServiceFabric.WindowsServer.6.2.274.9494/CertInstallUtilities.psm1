$ServerCertificateName = "CN=ServiceFabricServerCert"
$ClientCertificateName = "CN=ServiceFabricClientCert"

function IsSecurityX509([string]$ClusterConfigFilePath)
{
    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $properties = $jsonConfig.properties
    if ($properties -ne $Null)
    {
        $security = $properties.security
        if ($security -ne $Null )
        {
            $clusterCredentialType = $security.ClusterCredentialType
            return ($clusterCredentialType -ne $Null -And $clusterCredentialType -eq "X509")
        }
    }
    return $False
}

function IsLocalMachine([string] $MachineName)
{
    return ($MachineName -eq $env:ComputerName) -Or ($MachineName -eq "localhost")
}

function GetHostName([string] $iPAddress)
{
    $hostName = $iPAddress
    if ([bool]($iPAddress -as [ipaddress]))
    {
        $hostName = [System.Net.Dns]::GetHostByAddress($iPAddress).HostName
    }

    return $hostName
}

function IsThisMachineServer([string] $ClusterConfigFilePath)
{
    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $nodes = $jsonConfig.nodes

    Foreach ($node in $nodes)
    {
        $hostName = GetHostName -iPAddress $node.iPAddress

        if (IsLocalMachine -MachineName $hostName)
        {
            return $True;
        }
    }

    return $False
}

function InstallCertToLocal([string] $CertSetupScript)
{
    invoke-expression "& '$CertSetupScript' -Install -CertSubjectName '$ServerCertificateName'"
    invoke-expression "& '$CertSetupScript' -Install -CertSubjectName '$ClientCertificateName'"

    $cerLoc = "cert:\LocalMachine\My"
    $cert =  Get-ChildItem -Path $cerLoc | where { $_.Subject -like "*$ServerCertificateName*" }
    $serverthumbprint = $cert.Thumbprint

    $cert =  Get-ChildItem -Path $cerLoc | where { $_.Subject -like "*$ClientCertificateName*" }
    $clientthumbprint = $cert.Thumbprint
    
    return $serverthumbprint,$clientthumbprint
}

function IsOneBox([string] $ClusterConfigFilePath)
{
    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $nodes = $jsonConfig.nodes

    $machineNames = $nodes.iPAddress | select -uniq
    return ($machineNames.Count -eq 1)
}

function ExportCertificateToLocal([string]$PackageRoot, [string]$CertSetupScript, [string]$ServerThumbprint, [string]$ClientThumbprint, [string]$ClusterConfigFilePath)
{
    $serverCertificate = ExportCertificate -PackageRoot $packageRoot -Thumbprint $serverThumbprint -Name "server"
    $clientCertificate = ExportCertificate -PackageRoot $packageRoot -Thumbprint $clientThumbprint -Name "client"

    Write-Host "Server certificate is exported to $($serverCertificate[0]) with the password $($serverCertificate[1])"
    Write-Host "Client certificate is exported to $($clientCertificate[0]) with the password $($clientCertificate[1])"

    # If the current machine is server, then remove the client cert. If the current machine is client, then remove server cert.
    if (IsThisMachineServer -ClusterConfigFilePath $ClusterConfigFilePath)
    {
        if (-Not (IsOneBox -ClusterConfigFilePath $ClusterConfigFilePath))
        {
            Write-Host "Remove client certificate on localhost."
            invoke-expression "& '$CertSetupScript' -Clean -CertSubjectName '$ClientCertificateName'"
        }
    }
    else 
    {
        Write-Host "Remove server certificate on localhost."
        invoke-expression "& '$CertSetupScript' -Clean -CertSubjectName '$ServerCertificateName'"
    }

    return $serverCertificate
}

function ExportCertificate([string]$PackageRoot, [string]$Thumbprint, [string] $Name)
{
    $OutputCertificateFolder = Join-Path $PackageRoot "Certificates"
    if (-Not(Test-Path $OutputCertificateFolder))
    {
        New-Item -ItemType Directory -Force -Path $OutputCertificateFolder > $null
    }

    $PfxFilePath = "$OutputCertificateFolder\$Name.pfx"
    $randomnum = Get-Random
    $pswd = ConvertTo-SecureString -String "$randomnum" -Force -AsPlainText
    
    Get-ChildItem -Path "cert:\localMachine\my\$Thumbprint" | Export-PfxCertificate -FilePath $PfxFilePath -Password $pswd > $null

    return $PfxFilePath,$randomnum
}

function ModifyJsonThumbprint([string]$ClusterConfigFilePath, [string]$ServerThumbprint, [string]$ClientThumbprint, [string]$OutputPath)
{
    Write-Host "Modify thumbprint in $ClusterConfigFilePath"

    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $securityCertInfo = New-Object -TypeName PSObject

    $certificate= New-Object –TypeName PSObject
    $certificate | Add-Member -Name "Thumbprint" -value "$ServerThumbprint" -MemberType NoteProperty
    $certificate | Add-Member -Name "X509StoreName" -value "My" -MemberType NoteProperty

    $clientcertificate = New-Object -TypeName PSObject
    $clientcertificate | Add-Member -Name "CertificateThumbprint" -value "$ClientThumbprint" -MemberType NoteProperty
    $clientcertificate | Add-Member -Name "IsAdmin" -value $true -MemberType NoteProperty
    $clientcertificateArray = @()
    $clientcertificateArray += $clientcertificate

    $securityCertInfo | Add-Member -Name "ClusterCertificate" -value $certificate -MemberType NoteProperty
    $securityCertInfo | Add-Member -Name "ServerCertificate" -value $certificate -MemberType NoteProperty
    $securityCertInfo | Add-Member -Name "ClientCertificateThumbprints" -value $clientcertificateArray -MemberType NoteProperty

    if (IsReverseProxyEndPointConfigured -ClusterConfigFilePath $ClusterConfigFilePath)
    {
        $securityCertInfo | Add-Member -Name "ReverseProxyCertificate" -value $certificate -MemberType NoteProperty
    }

    $jsonConfig.properties.security.CertificateInformation = $securityCertInfo
    $jsonObject = ConvertTo-Json $jsonConfig -Depth 10
    $jsonObject > $OutputPath
}

function IsReverseProxyEndPointConfigured([string]$ClusterConfigFilePath)
{
    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $nodes = $jsonConfig.nodes
    $properties = $jsonConfig.properties
    $nodetypes = $properties.nodetypes

    foreach($node in $nodes)
    {
        $nodetypeRef = $node.nodetypeRef
        foreach($nodetype in $nodetypes)
        {
            if ($nodetype.Name -eq $nodetypeRef)
            {
                if ($nodetype.reverseProxyEndpointPort -ne $Null)
                {
                    return $True;
                }
            }
        }
    }

    return $False
}

function AddToTrustedHosts([string]$MachineName)
{
    Write-Host "Adding $MachineName to TrustedHosts"
    Set-Item WSMan:\localhost\Client\TrustedHosts -Value "$MachineName" -Concatenate -Force
}

function InstallCertToRemote([string]$ClusterConfigFilePath, [string]$CertificatePath, [string]$Password)
{
    $jsonConfig = Get-Content $ClusterConfigFilePath -Raw | ConvertFrom-Json
    $nodes = $jsonConfig.nodes
    $pswd = ConvertTo-SecureString -String $Password -Force -AsPlainText
    $certBytes = [System.IO.File]::ReadAllBytes($CertificatePath)

    $iPAddressList = $nodes.iPAddress | select -uniq

    Foreach ($iPAddress in $iPAddressList)
    {
        $machineName = GetHostName -iPAddress $iPAddress

        if (IsLocalMachine -MachineName $machineName)
        {
            Write-Host "Skipping certificate installation in $machineName."
            continue
        }

        AddToTrustedHosts -MachineName $machineName

        Write-Host "Connectting to $machineName"

        for($retry = 0; $retry -le 2; $retry ++)
        {
            Write-Host "Installing server certificate in $machineName for iteration $retry"
            Invoke-Command -ComputerName $machineName -ScriptBlock {
                param($pswd, $certBytes)
                Get-ChildItem -Path Cert:\LocalMachine\My | ? { $_.Subject -like "*$ServerCertificateName*" } | Remove-Item -Force
                Get-ChildItem -Path Cert:\LocalMachine\root | ? { $_.Subject -like "*$ServerCertificateName*" } | Remove-Item -Force
                Get-ChildItem -Path Cert:\CurrentUser\My | ? { $_.Subject -like "*$ServerCertificateName*" } | Remove-Item -Force

                $certPath = [System.IO.Path]::GetTempFileName()
                [system.IO.file]::WriteAllBytes($certPath, $certBytes);

                Import-PfxCertificate -Exportable -CertStoreLocation Cert:\LocalMachine\My -FilePath $certPath -Password $pswd > $null   
                Import-PfxCertificate -Exportable -CertStoreLocation Cert:\LocalMachine\root -FilePath $certPath -Password $pswd > $null  
                Import-PfxCertificate -Exportable -CertStoreLocation Cert:\CurrentUser\My -FilePath $certPath -Password $pswd > $null  

            } -ArgumentList $pswd,$certBytes

            if ($?)
            {
                Write-Host "Installed server certificate in $machineName"
                break
            }

            if ($retry -eq 2)
            {
                Write-Host "Installing server certificate in $machineName failed after 3 attemps"
            }
            else 
            {
                Write-Host "Unable to intall server certificate in $machineName, retry after 30 seconds..."
                Start-Sleep -Seconds 30
            } 
        }        
    }  
}
# SIG # Begin signature block
# MIIdjwYJKoZIhvcNAQcCoIIdgDCCHXwCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUA9N5lOI398Qw7sSN8QBflzhp
# H/mgghhTMIIEwTCCA6mgAwIBAgITMwAAAMKgCcU3dun2zQAAAAAAwjANBgkqhkiG
# 9w0BAQUFADB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEw
# HwYDVQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EwHhcNMTYwOTA3MTc1ODUx
# WhcNMTgwOTA3MTc1ODUxWjCBsTELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEMMAoGA1UECxMDQU9DMSYwJAYDVQQLEx1UaGFsZXMgVFNTIEVTTjpD
# M0IwLTBGNkEtNDExMTElMCMGA1UEAxMcTWljcm9zb2Z0IFRpbWUtU3RhbXAgU2Vy
# dmljZTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAJzfPT5gT5YLgF72
# 8Ipv/kMSm0FRtZmMMXMdDBrWM+LOObrNAITBA0w185w4qccTOzXIgsFlOyvvyGfI
# jH+4zLekfpL8U7DuccyDVdS3Lg70hYBCEJll0SwAhfpHR1D4NQaeIRnhnlRuSUwy
# 7LqOxCE6If90dH0+OaVlxiKHw7R5RgeO50m15BHI+6v9US70IZ8JFqRkfLpk52bh
# LNfnossW+CHvAFPVQ0uThMOaoESnJsmban0QaExZvftxreTrz2QQcVw74Y29CYbZ
# RUTIy4zIpuM/i5oBLj9mwf9CogC0rQibwWfEvPyiFuOZ/ncDX5I8KVHa4Y1LoFQq
# YWk/EEkCAwEAAaOCAQkwggEFMB0GA1UdDgQWBBTjHnnY/MhgLBEZmBJtobBujc6d
# rDAfBgNVHSMEGDAWgBQjNPjZUkZwCu1A+3b7syuwwzWzDzBUBgNVHR8ETTBLMEmg
# R6BFhkNodHRwOi8vY3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9N
# aWNyb3NvZnRUaW1lU3RhbXBQQ0EuY3JsMFgGCCsGAQUFBwEBBEwwSjBIBggrBgEF
# BQcwAoY8aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraS9jZXJ0cy9NaWNyb3Nv
# ZnRUaW1lU3RhbXBQQ0EuY3J0MBMGA1UdJQQMMAoGCCsGAQUFBwMIMA0GCSqGSIb3
# DQEBBQUAA4IBAQAoNFRrsA/+bdu8IJvKoxcry0vIPw0qzrUya7ud9MrJ/pp9EO01
# OFrXqbFfuPW0niqZt7hYrs7bzwSlmbBItCkImv0GCLS/3cf0Vl/c0NxUpn8TUjoo
# +qwnPF3qRGUzcwrI/3Xl9EfoDlc8jWd2f5FqrjeQdmkdOUmtxSnVt1kbW+Fnjlyl
# 1q8aWpkXXgNrBD29iXQV7BklsvtzSVLB32UTZqADm/yzqPC+osWN2eHED2nag1w0
# 51bq++5Pc2mA/UbJeqv+J9VhQwyTGoFdCjE9ygfd7aASPsxiAsRBsNRlylFMjePA
# nFZyI0P0rM+CW09Q641SEKIKbT6T1ww+8ByJMIIGATCCA+mgAwIBAgITMwAAAMTp
# ifh6gVDp/wAAAAAAxDANBgkqhkiG9w0BAQsFADB+MQswCQYDVQQGEwJVUzETMBEG
# A1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWlj
# cm9zb2Z0IENvcnBvcmF0aW9uMSgwJgYDVQQDEx9NaWNyb3NvZnQgQ29kZSBTaWdu
# aW5nIFBDQSAyMDExMB4XDTE3MDgxMTIwMjAyNFoXDTE4MDgxMTIwMjAyNFowdDEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEeMBwGA1UEAxMVTWlj
# cm9zb2Z0IENvcnBvcmF0aW9uMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKC
# AQEAiIq4JMMHj5qAeRX8JmD8cogs+vSjl4iWRrejy1+JLzozLh6RePp8qR+CAbV6
# yxq8A8pG68WZ9/sEHfKFCv8ibqHyZz3FJxjlKB/1BJRBY+zjuhWM7ROaNd44cFRv
# O+ytRQkwScG+jzCZDMt2yfdzlRZ30Yu7lMcIhSDtHqg18XHC4HQAS4rS3JHr1nj+
# jfqtYIg9vbkfrmKXv8WEsZCu1q8r01T7NdrNcZLmHv/scWvLfwh2dOAQUUjU8QDI
# SEyjBzXlWQ39fJzI5lrjhfXWmg8fjqbkhBfB1sqfHQHH/UinE5IzlyFIMvjCJKIA
# sr5TyoNuKVuB7zhugPO77BML6wIDAQABo4IBgDCCAXwwHwYDVR0lBBgwFgYKKwYB
# BAGCN0wIAQYIKwYBBQUHAwMwHQYDVR0OBBYEFMvWYoTPYDnq/2fCXNLIu6u3wxOY
# MFIGA1UdEQRLMEmkRzBFMQ0wCwYDVQQLEwRNT1BSMTQwMgYDVQQFEysyMzAwMTIr
# YzgwNGI1ZWEtNDliNC00MjM4LTgzNjItZDg1MWZhMjI1NGZjMB8GA1UdIwQYMBaA
# FEhuZOVQBdOCqhc3NyK1bajKdQKVMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly93
# d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY0NvZFNpZ1BDQTIwMTFfMjAx
# MS0wNy0wOC5jcmwwYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8v
# d3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY0NvZFNpZ1BDQTIwMTFf
# MjAxMS0wNy0wOC5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAgEA
# BhYf21fCUMgjT6JReNft+P3NvdXA8fkbVu1TyGlHBdXEy+zi/JlblV8ROCjABUUT
# 4Jp5iLxmq9u76wJVI7c9I3hBba748QBalJmKHMwJldCaHEQwqaUWx7pHW/UrNIuf
# j1g3w04cryLKEM3YghCpNfCuIsiPJKaBi98nHORmHYk+Lv9XA03BboOgMuu0sy9Q
# Vl0GsRWMyB1jt3MM49Z6Jg8qlkWnMoM+lj5XSXcjif6xEMeK5QgVUcUrWjFbOWqW
# qKSIa5Yob/HEruq9RRfMYk6BtVQaR46YpW3AbifG+CcfyO0gqQux8c4LmpTiap1p
# g6E2120g/oXV/8O4lzYJ/j0UwZgUqcCGzO+CwatVJEMYtUiFeIbQ+dKdPxnZFInn
# jZ9oJIhoO6nHgE4m5wghTGP9nJMVTTO1VmBP10q5OI7/Lt2xX6RDa8l4z7G7a4+D
# bIdyquql+5/dGtY5/GTJbT4I5XyDsa28o7p7z5ZWpHpYyxJHYtIh7/w8xDEL9y8+
# ZKU3b2BQP7dEkE+gC4u+flj2x2eHYduemMTIjMtvR+HALpTtsfawMG3sakmo6ZZ2
# yL0IxP479a5zNwayVs8Z1Lv1lMqHHPKAagFPthuBc7PTWyI/OlgY34juZ8RJpy/c
# JYs9XtDsNESRHbyRDHaCPu/E2C2hBAKOSPnv3QLPA6IwggYHMIID76ADAgECAgph
# Fmg0AAAAAAAcMA0GCSqGSIb3DQEBBQUAMF8xEzARBgoJkiaJk/IsZAEZFgNjb20x
# GTAXBgoJkiaJk/IsZAEZFgltaWNyb3NvZnQxLTArBgNVBAMTJE1pY3Jvc29mdCBS
# b290IENlcnRpZmljYXRlIEF1dGhvcml0eTAeFw0wNzA0MDMxMjUzMDlaFw0yMTA0
# MDMxMzAzMDlaMHcxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAw
# DgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# ITAfBgNVBAMTGE1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQTCCASIwDQYJKoZIhvcN
# AQEBBQADggEPADCCAQoCggEBAJ+hbLHf20iSKnxrLhnhveLjxZlRI1Ctzt0YTiQP
# 7tGn0UytdDAgEesH1VSVFUmUG0KSrphcMCbaAGvoe73siQcP9w4EmPCJzB/LMySH
# nfL0Zxws/HvniB3q506jocEjU8qN+kXPCdBer9CwQgSi+aZsk2fXKNxGU7CG0OUo
# Ri4nrIZPVVIM5AMs+2qQkDBuh/NZMJ36ftaXs+ghl3740hPzCLdTbVK0RZCfSABK
# R2YRJylmqJfk0waBSqL5hKcRRxQJgp+E7VV4/gGaHVAIhQAQMEbtt94jRrvELVSf
# rx54QTF3zJvfO4OToWECtR0Nsfz3m7IBziJLVP/5BcPCIAsCAwEAAaOCAaswggGn
# MA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFCM0+NlSRnAK7UD7dvuzK7DDNbMP
# MAsGA1UdDwQEAwIBhjAQBgkrBgEEAYI3FQEEAwIBADCBmAYDVR0jBIGQMIGNgBQO
# rIJgQFYnl+UlE/wq4QpTlVnkpKFjpGEwXzETMBEGCgmSJomT8ixkARkWA2NvbTEZ
# MBcGCgmSJomT8ixkARkWCW1pY3Jvc29mdDEtMCsGA1UEAxMkTWljcm9zb2Z0IFJv
# b3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5ghB5rRahSqClrUxzWPQHEy5lMFAGA1Ud
# HwRJMEcwRaBDoEGGP2h0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3By
# b2R1Y3RzL21pY3Jvc29mdHJvb3RjZXJ0LmNybDBUBggrBgEFBQcBAQRIMEYwRAYI
# KwYBBQUHMAKGOGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWlj
# cm9zb2Z0Um9vdENlcnQuY3J0MBMGA1UdJQQMMAoGCCsGAQUFBwMIMA0GCSqGSIb3
# DQEBBQUAA4ICAQAQl4rDXANENt3ptK132855UU0BsS50cVttDBOrzr57j7gu1BKi
# jG1iuFcCy04gE1CZ3XpA4le7r1iaHOEdAYasu3jyi9DsOwHu4r6PCgXIjUji8FMV
# 3U+rkuTnjWrVgMHmlPIGL4UD6ZEqJCJw+/b85HiZLg33B+JwvBhOnY5rCnKVuKE5
# nGctxVEO6mJcPxaYiyA/4gcaMvnMMUp2MT0rcgvI6nA9/4UKE9/CCmGO8Ne4F+tO
# i3/FNSteo7/rvH0LQnvUU3Ih7jDKu3hlXFsBFwoUDtLaFJj1PLlmWLMtL+f5hYbM
# UVbonXCUbKw5TNT2eb+qGHpiKe+imyk0BncaYsk9Hm0fgvALxyy7z0Oz5fnsfbXj
# pKh0NbhOxXEjEiZ2CzxSjHFaRkMUvLOzsE1nyJ9C/4B5IYCeFTBm6EISXhrIniIh
# 0EPpK+m79EjMLNTYMoBMJipIJF9a6lbvpt6Znco6b72BJ3QGEe52Ib+bgsEnVLax
# aj2JoXZhtG6hE6a/qkfwEm/9ijJssv7fUciMI8lmvZ0dhxJkAj0tr1mPuOQh5bWw
# ymO0eFQF1EEuUKyUsKV4q7OglnUa2ZKHE3UiLzKoCG6gW4wlv6DvhMoh1useT8ma
# 7kng9wFlb4kLfchpyOZu6qeXzjEp/w7FW1zYTRuh2Povnj8uVRZryROj/TCCB3ow
# ggVioAMCAQICCmEOkNIAAAAAAAMwDQYJKoZIhvcNAQELBQAwgYgxCzAJBgNVBAYT
# AlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYD
# VQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xMjAwBgNVBAMTKU1pY3Jvc29mdCBS
# b290IENlcnRpZmljYXRlIEF1dGhvcml0eSAyMDExMB4XDTExMDcwODIwNTkwOVoX
# DTI2MDcwODIxMDkwOVowfjELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0
# b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3Jh
# dGlvbjEoMCYGA1UEAxMfTWljcm9zb2Z0IENvZGUgU2lnbmluZyBQQ0EgMjAxMTCC
# AiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKvw+nIQHC6t2G6qghBNNLry
# tlghn0IbKmvpWlCquAY4GgRJun/DDB7dN2vGEtgL8DjCmQawyDnVARQxQtOJDXlk
# h36UYCRsr55JnOloXtLfm1OyCizDr9mpK656Ca/XllnKYBoF6WZ26DJSJhIv56sI
# UM+zRLdd2MQuA3WraPPLbfM6XKEW9Ea64DhkrG5kNXimoGMPLdNAk/jj3gcN1Vx5
# pUkp5w2+oBN3vpQ97/vjK1oQH01WKKJ6cuASOrdJXtjt7UORg9l7snuGG9k+sYxd
# 6IlPhBryoS9Z5JA7La4zWMW3Pv4y07MDPbGyr5I4ftKdgCz1TlaRITUlwzluZH9T
# upwPrRkjhMv0ugOGjfdf8NBSv4yUh7zAIXQlXxgotswnKDglmDlKNs98sZKuHCOn
# qWbsYR9q4ShJnV+I4iVd0yFLPlLEtVc/JAPw0XpbL9Uj43BdD1FGd7P4AOG8rAKC
# X9vAFbO9G9RVS+c5oQ/pI0m8GLhEfEXkwcNyeuBy5yTfv0aZxe/CHFfbg43sTUkw
# p6uO3+xbn6/83bBm4sGXgXvt1u1L50kppxMopqd9Z4DmimJ4X7IvhNdXnFy/dygo
# 8e1twyiPLI9AN0/B4YVEicQJTMXUpUMvdJX3bvh4IFgsE11glZo+TzOE2rCIF96e
# TvSWsLxGoGyY0uDWiIwLAgMBAAGjggHtMIIB6TAQBgkrBgEEAYI3FQEEAwIBADAd
# BgNVHQ4EFgQUSG5k5VAF04KqFzc3IrVtqMp1ApUwGQYJKwYBBAGCNxQCBAweCgBT
# AHUAYgBDAEEwCwYDVR0PBAQDAgGGMA8GA1UdEwEB/wQFMAMBAf8wHwYDVR0jBBgw
# FoAUci06AjGQQ7kUBU7h6qfHMdEjiTQwWgYDVR0fBFMwUTBPoE2gS4ZJaHR0cDov
# L2NybC5taWNyb3NvZnQuY29tL3BraS9jcmwvcHJvZHVjdHMvTWljUm9vQ2VyQXV0
# MjAxMV8yMDExXzAzXzIyLmNybDBeBggrBgEFBQcBAQRSMFAwTgYIKwYBBQUHMAKG
# Qmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljUm9vQ2VyQXV0
# MjAxMV8yMDExXzAzXzIyLmNydDCBnwYDVR0gBIGXMIGUMIGRBgkrBgEEAYI3LgMw
# gYMwPwYIKwYBBQUHAgEWM2h0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMv
# ZG9jcy9wcmltYXJ5Y3BzLmh0bTBABggrBgEFBQcCAjA0HjIgHQBMAGUAZwBhAGwA
# XwBwAG8AbABpAGMAeQBfAHMAdABhAHQAZQBtAGUAbgB0AC4gHTANBgkqhkiG9w0B
# AQsFAAOCAgEAZ/KGpZjgVHkaLtPYdGcimwuWEeFjkplCln3SeQyQwWVfLiw++MNy
# 0W2D/r4/6ArKO79HqaPzadtjvyI1pZddZYSQfYtGUFXYDJJ80hpLHPM8QotS0LD9
# a+M+By4pm+Y9G6XUtR13lDni6WTJRD14eiPzE32mkHSDjfTLJgJGKsKKELukqQUM
# m+1o+mgulaAqPyprWEljHwlpblqYluSD9MCP80Yr3vw70L01724lruWvJ+3Q3fMO
# r5kol5hNDj0L8giJ1h/DMhji8MUtzluetEk5CsYKwsatruWy2dsViFFFWDgycSca
# f7H0J/jeLDogaZiyWYlobm+nt3TDQAUGpgEqKD6CPxNNZgvAs0314Y9/HG8VfUWn
# duVAKmWjw11SYobDHWM2l4bf2vP48hahmifhzaWX0O5dY0HjWwechz4GdwbRBrF1
# HxS+YWG18NzGGwS+30HHDiju3mUv7Jf2oVyW2ADWoUa9WfOXpQlLSBCZgB/QACnF
# sZulP0V3HjXG0qKin3p6IvpIlR+r+0cjgPWe+L9rt0uX4ut1eBrs6jeZeRhL/9az
# I2h15q/6/IvrC4DqaTuv/DDtBEyO3991bWORPdGdVk5Pv4BXIqF4ETIheu9BCrE/
# +6jMpF3BoYibV3FWTkhFwELJm3ZbCoBIa/15n8G9bW1qyVJzEw16UM0xggSmMIIE
# ogIBATCBlTB+MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4G
# A1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSgw
# JgYDVQQDEx9NaWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQSAyMDExAhMzAAAAxOmJ
# +HqBUOn/AAAAAADEMAkGBSsOAwIaBQCggbowGQYJKoZIhvcNAQkDMQwGCisGAQQB
# gjcCAQQwHAYKKwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwIwYJKoZIhvcNAQkE
# MRYEFB5gzrcE29+0E5zQ0QCCNyO/erOvMFoGCisGAQQBgjcCAQwxTDBKoCSAIgBN
# AGkAYwByAG8AcwBvAGYAdAAgAFcAaQBuAGQAbwB3AHOhIoAgaHR0cDovL3d3dy5t
# aWNyb3NvZnQuY29tL3dpbmRvd3MwDQYJKoZIhvcNAQEBBQAEggEAh9V/Ve+7gTyc
# 123a4K+A4Nvis92ROePW/5sCfZ1+XfFUe+1ha+9BtrNW//IWg338tpfnwid86ipT
# L7pZrM9mag8n+LA5ewZ9OxquVZNwQd/sxOBVb7i/JZOLhiZIE6YsIKshK1zrymVh
# 1ssbCDYB96JAeOEAc5iq6inFxerp/c2QtXavBJascew0GdjWegcQc4bnA4WjJwfV
# ZMvYvASFAUY1AlhQLmBEJHySCDG249LMyft1hGI/8o61ZHwIEx57H4rs0JounvSM
# E4opARdYFiGkAomp1dE2lfc1xIF+mF93pP0jyWIPLfCs0Xzu9nctXhI0KWBwO0gR
# rf1fzxUKU6GCAigwggIkBgkqhkiG9w0BCQYxggIVMIICEQIBATCBjjB3MQswCQYD
# VQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEe
# MBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3Nv
# ZnQgVGltZS1TdGFtcCBQQ0ECEzMAAADCoAnFN3bp9s0AAAAAAMIwCQYFKw4DAhoF
# AKBdMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTE4
# MDUwMzA5MDQyN1owIwYJKoZIhvcNAQkEMRYEFFdn1lrUxYT/LrSwLvGgsyrqmGOo
# MA0GCSqGSIb3DQEBBQUABIIBAIMe71qdbLOSP0vq1KW9nXyQS6OCG1oPb9Vfdi9R
# uoSdjMD3FIYUZmRpuUT5dtHO1VQ90LiIgtfvehdSxSyVCu370G4G+Vb4dET0d8Uu
# h1EI9PUiNCkm9pEH7QJ98pBWlBWPNYv1Jfc84jYsPiLZWnALjqsazkCxJBm17ct1
# sv7E3lYlsUnAjD/09x9ZA+2n3RqNSmu9EWduxQH/MdAuzcPvPyhbFh4htNDqgMsI
# d+w+lGym0xtTszE4gShv7p//kI/6EI+TwBIuqzwtXnUOQk7E1aO7s6tjE4HfCvdt
# mXzxPNdBaRRjnr/m3XKhMoWFkIu8aSHivKNEozj4mqZnz+4=
# SIG # End signature block
