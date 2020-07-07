#! /bin/bash

basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}"

repodir=/h/gitrep/azure-docs-pr/
custrepodir=/h/gitrep/mc-docs-pr.en-us/
sourcetreedir=/h/gitrep/SourceTreeScript/SourceTreeScript/

# Step 1: Go to the global azure-docs-pr repostory and generate file to save the change files list.
cd ${repodir}

if [ -d ${repodir} ];then
	echo -e "\n\rChange to directory : "${repodir}
else
	echo -e "\n\rThe Application directory is not exists  - "${repodir}
fi

read -p "The Branch Checkout Last Month: branchLastMonth= " branchLastMonth
read -p "The Branch Checkout This Month: branchThisMonth= " branchThisMonth
read -p "Do you only get the file change list report? [N/Y]: the default is N.  fullProcess= " fullProcess



timestamp=`date +%Y%m%d%H%M$S`
generatename="changeFileList_${timestamp}.txt"
generatenamewithstatus="changeFileList_${timestamp}_status.txt"

changelistfile="${repodir}${generatename}"
changestatusfile="${repodir}${generatenamewithstatus}"
edwardstatusfile="${repodir}$edwardfilelist"

echo -e "\n\rGenerate the download file name: "${changelistfile}

# Step 2: Generate the change file list with git diff command via the pip command.
git diff $branchLastMonth $branchThisMonth |\
 grep "diff --git" |\
 grep -E -o "(a|b)/(articles/(aks|analysis\-service|azure\-resource\-manager|container\-registry|cosmos\-db|firewall|\
       service\-fabric|site\-recovery|sql\-strentch\-db|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan)/|\
       includes/(?!(active\-directory|advisor|api\-management|application\-gateway|app\-service|automation|azure\-cache|azure\-function|\
	   azure\-monitor|azure\-portal|azure\-stack|backup|batch|cloud\-services|cognitive\-services|connectors|databox|data\-explorer|data\-factory|\
	   dms|dns|event\-grid|event\-hubs|expressroute|governance|guides|hdinsight|iot|key\-vault|load\-balancer|logic\-apps|mariadb|media|monitoring|\
	   multi\-factor\-authentication|mysql|networking|network\-watcher|notification\-hubs|postgresql|power\-bi|role\-base\-access\-control|schedule|\
	   search|security|service\-bus|service\-health|sql\-data|stream\-analytics|times\-series\-insights|vpn\-gateway)))\S*\.(md|yml)" |\
 cut -c 3- | sort | uniq  >> ${changelistfile}


if [ -f ${changelistfile} ];then
	echo -e "\n\r${changelistfile} have been generated successfully!"
else
	echo -e "\n\r${changelistfile} have been generated failed!"
fi

echo -e "\n\rCheck the change list file status..... "

# Step 3: Got the unsuitable file list in MOONCAKE portal.
exceptlistfile="${basedir}/mooncake_unsuitable_filename.txt"

if [ -f "${exceptlistfile}" ];then
	echo -e "\n\rSearch ${exceptlistfile} successfully!"
else
	echo -e "\n\rSearch ${exceptlistfile} failed!"
	exit -1
fi

# Step 4: Get the status for the change file list: NEW UPDATE UNSUITABLE.
while read line
do

	echo $line | grep -i $line "${exceptlistfile}"
	if [ $? -ne 0 ]; then
		if [ -f "${custrepodir}$line" ]; then
			echo -e "${line}\tUPDATE" >> ${changestatusfile}
		else
			echo -e "${line}\tNEW" >> ${changestatusfile}
		fi
	else
		# means this file is unsuitable content for mooncake.
		echo -e "${line}\tUNSUITABLE" >> ${changestatusfile}
	fi
	
done <${changelistfile}

echo -e "\n\rCheck the file status complate! Please refer to ${changestatusfile}."

if ([ ${fullProcess} = "N" ] || [ ${fullProcess} = "n" ]);then

	echo -e "\n\rWe have sign out the customize process temproary. After you have completed the month schedule,"
	echo -e "you can use go throught the full process later with following paremeter:"
	echo -e "\033[47;31m fullProcess = Y \033[0m"
	echo -e "\n\rThe application will log out in 5 second."
	sleep 5
	exit 0
fi

cd ${repodir}

# Step 5: Checkout the global sync branch and we will customize the articles on it.
git checkout $branchThisMonth

echo -e "\n\rCheckout the branch $branchThisMonth successfully!"

# Step 5: Get the status for the change file list: NEW UPDATE UNSUITABLE.
winsourcetreedir="D:\\gitrep\\SourceTreeScript\\SourceTreeScript\\"
winrepodirnodash="D:\\gitrep\\azure-docs-pr"

repodirnodash="/h/gitrep/azure-docs-pr/"
pythondir=/c/Python34/

changestatusfile="${basedir}/changeFileList_status.txt"

rowidx=0
roundidx=1

rowmin=0
rowmax=100

arrcustfile=()
fileconnect=""

# Step 6: Go to Jack's application root path and get ready to customize the articles batch by batch.

cd $pythondir

echo -e "\n\rChange to python.exe directory successfully!"

echo -e "\n\rStart to customize the file with Jack's app!"


while read line
do

	filekey=`echo "${line}" | cut -d "	" -f 1`
	filestaus=`echo "${line}" | cut -d "	" -f 2`

	# Step 6.1: We collect the files list for customize later, We only collect for status "NEW" and "UPDATE", discard the "UNSUITABLE" status.
	if ([ "$filestaus" = "NEW" ] || [ "$filestaus" = "UPDATE" ]);then
		arrcustfile[rowidx]="${filekey}"
		let rowidx+=1
	fi
	
	if [ "$rowidx" -eq "$rowmax" ];then
	
		# we will customize the files with one batch which contains 100.
		echo -e "\n\rCustomize the ${roundidx} batch files collection!"
		let roundidx+=1
	
		for((i=${rowmin};i<${rowmax};i++));
		do
			# File if exist, then connect to the parametes.
			
			if [ -f "${repodirnodash}${arrcustfile[i]}" ];then
				fileconnect="${fileconnect} ${arrcustfile[i]}"
			fi
		done
		
		if true;then
			#echo "fileconnect = ${fileconnect}"
			echo -e "\n\rStep 1: Replace the OS external code form repository."
			
			# Step 6.2: We invoke the Jack app for menu (Replace the OS external code)
			./python.exe ${winsourcetreedir}SourceTreeScript.py replace_script \
				${winrepodirnodash} D:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				D:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ D:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				D:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ D:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${fileconnect}
		fi
		
		if true;then
			echo -e "\n\rStep 2: Customize the articles with python app."
			# Step 6.3: We invoke the Jack app for menu (Customize the articles with Regular Expression)
			./python.exe ${winsourcetreedir}SourceTreeScript.py customize_files ${winrepodirnodash} ${fileconnect}
		fi
		
		fileconnect=""
		rowidx=0
	fi
	
done <"${changestatusfile}"

if [ ${rowidx} > ${rowmin} ];then
	
	# we will customize the files with the last batch.
	echo -e "\n\rCustomize the ${roundidx} batch\(Last Round\) files collection!"
	let roundidx+=1
		
	for((i=${rowmin};i<${rowidx};i++));
	do
		if [ -f "${repodirnodash}${arrcustfile[i]}" ];then
			fileconnect="${fileconnect} ${arrcustfile[i]}"
		fi
	done
		
	if true;then
		echo -e "\n\rStep 1: Replace the OS external code form repository."
		./python.exe ${winsourcetreedir}SourceTreeScript.py replace_script \
				${winrepodirnodash} D:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				D:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ D:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				D:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ D:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${fileconnect}
	fi
	
	if true;then
		echo -e "\n\rStep 2: Customize the articles with python app."
		./python.exe ${sourcetreedir}SourceTreeScript.py customize_files ${repodirnodash} ${fileconnect}
	fi
	
fi

cd $repodir

echo -e "\n\r Go to global sync branch - ${branchThisMonth} and wait for 15 second before save stash!"

sleep 15

# Step 7: Save the update files as Jack's customize on branch

git stash save "Jack Customize ${timestamp}"

git stach apply

# Step 8: Save the update files as Jack's customize on branch


sleep 15


edwardcustfile=/h/gitrep/Production/AuthorCustomization/AuthorCustomization/filelist.txt
edwardapppath=/h/gitrep/Production/AuthorCustomization/AuthorCustomization/bin/Release

cd $edwardapppath

./AuthorCustomization.exe -S F -C A

sleep 10

./AuthorCustomization.exe -S F -C U

sleep 10

./AuthorCustomization.exe -S F -C C

sleep 10

# Step 9: Save the update files as Jack's customize on branch

cd $repodir

echo -e "\n\r Go to global sync branch - ${branchThisMonth} and wait for 15 second before save stash!"

sleep 15

git stash save "Edward Customize ${timestamp}"

git stach apply

echo -e "Customzie the sync files successfully!"












