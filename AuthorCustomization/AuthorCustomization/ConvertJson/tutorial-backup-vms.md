---
title: '在 Azure 中备份 Windows 虚拟机 | Azure'
description: 使用 Azure 备份来备份 Windows VM，从而为其提供保护
services: virtual-machines-windows
documentationcenter: virtual-machines
author: rockboyfor
manager: digimobile
editor: tysonn
tags: azure-resource-manager

ms.assetid: 
ms.service: virtual-machines-windows
ms.devlang: na
ms.topic: article
ms.tgt_pltfrm: vm-linux
ms.workload: infrastructure
origin.date: 07/27/2017
ms.date: 01/05/2018
ms.author: v-yeche
ms.custom: mvc
---
# <a name="back-up-windows-virtual-machines-in-Azure"></a>在 Azure 中备份 Windows 虚拟机

可以通过定期创建备份来保护数据。 Azure 备份可创建恢复点，这些恢复点存储在异地冗余的恢复保管库中。 从恢复点还原时，可以还原整个 VM，也可以仅还原特定的文件。 本文介绍了如何将单个文件还原到运行 Windows Server 和 IIS 的 VM。 如果尚没有 VM 可使用，可以参考 [ Windows 快速入门](quick-create-portal.md). 本教程介绍如何执行下列操作：

> [!div class="checklist"]
> * 创建 VM 的备份
> * 计划每日备份
> * 从备份还原文件

## <a name="backup-overview"></a>备份概述

当 Azure 备份服务启动备份作业时，将触发备份扩展来创建时间点快照。 Azure 备份服务使用 VMSnapshot 扩展。 该扩展是在首次 VM 备份（如果 VM 正在运行）期间安装的。 如果 VM 未运行，备份服务将创建基础存储的快照（因为在 VM 停止时不会发生任何应用程序写入）。

创建 Windows VM 快照时，备份服务与卷影复制服务 (VSS) 互相配合，来获取虚拟机磁盘的一致性快照。 Azure 备份服务创建快照后，数据将传输到保管库。 为最大限度地提高效率，服务仅标识和传输自上次备份以后已更改的数据块。

数据传输完成后，会删除快照并创建恢复点。

## <a name="create-a-backup"></a>创建备份
在恢复服务保管库中创建一个简单的已计划每日备份。  

1. 登录到[Azure 门户](https://portal.azure.cn/)。
2. 在左侧菜单中选择“虚拟机”。
3. 从列表中选择要备份的 VM。
4. 在 VM 边栏选项卡上的“设置”部分中，单击“备份”。 此时会打开“启用备份”边栏选项卡。
5. 在“恢复服务保管库”中，单击“新建”并为新保管库提供名称。 将在与虚拟机相同的资源组和位置中创建新保管库。
6. 单击“备份策略”。 对于本示例，请保留默认值，并单击“确定”。
7. 在“启用备份”边栏选项卡中，单击“启用备份”。 这会根据默认的计划创建每日备份。
8. 若要创建初始恢复点，请在“备份”边栏选项卡中单击“立即备份”。
9. 在“立即备份”边栏选项卡中单击日历图标，使用日历控件选择保留此恢复点的最后一天，并单击“备份”。
10. 在 VM 的“备份”边栏选项卡中，可以看到已完成的恢复点数。

    ![Recovery points](./media/tutorial-backup-vms/backup-complete.png)

首次备份大约需要 20 分钟。 完成备份后，请继续执行本教程的下一部分。

## 恢复文件

如果意外删除或更改了某个文件，可以使用文件恢复从备份保管库恢复该文件。 文件恢复使用一个在 VM 上运行的脚本将恢复点装载为本地驱动器。 这些驱动器将保持装载 12 小时，以便可以从恢复点复制文件并将其还原到 VM。

本示例展示了如何恢复在 IIS 的默认网页中使用的图像文件。

1. 打开浏览器，并连接到 VM 的 IP 地址来显示默认 IIS 页面。

    ![Default IIS web page](./media/tutorial-backup-vms/iis-working.png)

2. 连接到 VM。
3. 在 VM 上，打开文件资源管理器并导航到 \inetpub\wwwroot 并删除 iisstart.png。
4. 在本地计算机上，刷新浏览器，检查默认 IIS 页面上的图像是否已消失。

    ![Default IIS web page](./media/tutorial-backup-vms/iis-broken.png)

5. 在本地计算机上，打开一个新选项卡，并转到 [Azure 门户](https://portal.azure.cn).
6. 在左侧菜单中，选择“虚拟机”，并从列表中选择 VM。
7. 在 VM 边栏选项卡上的“设置”部分中，单击“备份”。 此时会打开“备份”边栏选项卡。 
8. 在边栏选项卡顶部的菜单中，选择“文件恢复”。 此时会打开“文件恢复”边栏选项卡。
9. 在“步骤 1: 选择恢复点”中，从下拉列表中选择恢复点。
10. 在“步骤 2: 下载脚本以浏览并恢复文件”中，单击“下载可执行文件”按钮。 将文件保存到下载文件夹。
11. 在本地计算机上，打开文件资源管理器，导航到下载文件夹并复制所下载的 .exe 文件。 文件名以 VM 名称作为前缀。 
12. 在 VM 上（通过 RDP 连接），将该 .exe 文件粘贴到 VM 的桌面。 
13. 导航到 VM 的桌面并双击该 .exe 文件。 这会启动一个命令提示符，然后将恢复点装载为可以访问的文件共享。 完成该共享创建时，键入 q 以关闭命令提示符。
14. 在 VM 上，打开文件资源管理器，导航到用于该文件共享的驱动器号。
15. 导航到 \inetpub\wwwroot，从文件共享中复制 iisstart.png 并将其粘贴到 \inetpub\wwwroot 中。 例如，复制 F:\inetpub\wwwroot\iisstart.png 并将其粘贴到 c:\inetpub\wwwroot 中以恢复该文件。
16. 在本地计算机上，打开从中连接到 VM 的 IP 地址的浏览器选项卡，其中显示了 IIS 默认页面。 按 CTRL + F5 刷新浏览器页面。 现在，应该会看到图像已还原。
17. 在本地计算机上，返回到 Azure 门户的浏览器选项卡，并在“步骤 3: 恢复后卸载磁盘”中单击“卸载磁盘”按钮。 如果忘记执行此步骤，与装入点的连接会在 12 小时后自动关闭。 在这 12 个小时后，若要创建新的装入点，需要下载新脚本。

## 后续步骤

在本教程中，已学习了如何执行以下操作

> [!div class="checklist"]
> * 创建 VM 的备份
> * 计划每日备份
> * 从备份还原文件

请转到下一教程，了解如何监视虚拟机。

> [!div class="nextstepaction"]
> [监视虚拟机](tutorial-monitoring.md)
