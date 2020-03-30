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

read -p "Provider branch name or SHA last month: branchLastMonth= " branchLastMonth
read -p "Provider branch name or SHA this month: branchThisMonth= " branchThisMonth
read -p "Full process or not? [N/Y]: the default is N.  fullProcess= " fullProcess

if [ ! $fullProcess ]; then
	#echo "fullProcess is ${fullProcess}!"
	fullProcess="N"
fi

timestamp=`date +%Y%m%d%H%M$S`
generatename="changeFileList_${timestamp}.txt"
generatenamewithstatus="changeFileList_${timestamp}_status.txt"

changelistfile="${repodir}${generatename}"
changestatusfile="${repodir}${generatenamewithstatus}"
edwardstatusfile="${repodir}$edwardfilelist"

echo -e "\n\rGenerate the download file name: "${changelistfile}

# Step 2.0: Configure the diff.renamelist parameter to discard the warning message.

git config diff.renamelimit 99999999

# Step 2: Generate the change file list with git diff command via the pip command.
git diff $branchLastMonth $branchThisMonth --diff-filter=AMDR --name-only |\
grep -E -o "(articles/(aks|analysis\-services|azure\-resource\-manager|container\-instances|container\-registry|cosmos\-db|firewall|private\-link|logic\-apps|connectors|\
service\-fabric|site\-recovery|sql\-server\-stretch\-database|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan)/|\
includes/)\S*\.(md|yml)" |\
sort | uniq   >> ${changelistfile}

# Step 2.2 Remove the other service includes files.

sed -i '/^articles\/virtual\-machines\/workloads/'d ${changelistfile}

sed -i '/^includes\/active\-directory/'d ${changelistfile}
sed -i '/^includes\/advisor/'d ${changelistfile}
sed -i '/^includes\/api\-management/'d ${changelistfile}
sed -i '/^includes\/application\-gateway/'d ${changelistfile}
sed -i '/^includes\/app\-service/'d ${changelistfile}
sed -i '/^includes\/automation/'d ${changelistfile}
sed -i '/^includes\/azure\-cache/'d ${changelistfile}
sed -i '/^includes\/azure\-function/'d ${changelistfile}
sed -i '/^includes\/azure\-monitor/'d ${changelistfile}
sed -i '/^includes\/azure\-portal/'d ${changelistfile}
sed -i '/^includes\/azure\-stack/'d ${changelistfile}
sed -i '/^includes\/backup/'d ${changelistfile}
sed -i '/^includes\/batch/'d ${changelistfile}
sed -i '/^includes\/cloud\-services/'d ${changelistfile}
sed -i '/^includes\/cognitive\-services/'d ${changelistfile}
sed -i '/^includes\/databox/'d ${changelistfile}
sed -i '/^includes\/data\-explorer/'d ${changelistfile}
sed -i '/^includes\/data\-factory/'d ${changelistfile}
sed -i '/^includes\/dms/'d ${changelistfile}
sed -i '/^includes\/dns/'d ${changelistfile}
sed -i '/^includes\/event\-grid/'d ${changelistfile}
sed -i '/^includes\/event\-hubs/'d ${changelistfile}
sed -i '/^includes\/expressroute/'d ${changelistfile}
sed -i '/^includes\/governance/'d ${changelistfile}
sed -i '/^includes\/guides/'d ${changelistfile}
sed -i '/^includes\/hdinsight/'d ${changelistfile}
sed -i '/^includes\/iot/'d ${changelistfile}
sed -i '/^includes\/key\-vault/'d ${changelistfile}
sed -i '/^includes\/load\-balancer/'d ${changelistfile}
sed -i '/^includes\/mariadb/'d ${changelistfile}
sed -i '/^includes\/media/'d ${changelistfile}
sed -i '/^includes\/monitoring/'d ${changelistfile}
sed -i '/^includes\/multi\-factor\-authentication/'d ${changelistfile}
sed -i '/^includes\/mysql/'d ${changelistfile}
sed -i '/^includes\/networking/'d ${changelistfile}
sed -i '/^includes\/network\-watcher/'d ${changelistfile}
sed -i '/^includes\/notification\-hubs/'d ${changelistfile}
sed -i '/^includes\/power\-bi/'d ${changelistfile}
sed -i '/^includes\/role\-base\-access\-control/'d ${changelistfile}
sed -i '/^includes\/schedule/'d ${changelistfile}
sed -i '/^includes\/security/'d ${changelistfile}
sed -i '/^includes\/service\-bus/'d ${changelistfile}
sed -i '/^includes\/service\-health/'d ${changelistfile}
sed -i '/^includes\/sql\-data/'d ${changelistfile}
sed -i '/^includes\/stream\-analytics/'d ${changelistfile}
sed -i '/^includes\/times\-series\-insights/'d ${changelistfile}
sed -i '/^includes\/vpn\-gateway/'d ${changelistfile}



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
			if [ -f "${repodir}$line" ]; then
				# Change the status to UPDARE when the global files exists.
				echo -e "${line}\tUPDATE" >> ${changestatusfile}
			else
				# Change the status to DELETE when the global files NOT exists.
				echo -e "${line}\tDELETE" >> ${changestatusfile}
			fi
			
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
winsourcetreedir="H:\\gitrep\\SourceTreeScript\\SourceTreeScript\\"
winrepodirnodash="H:\\gitrep\\azure-docs-pr"

repodirnodash="/h/gitrep/azure-docs-pr/"
pythondir=/c/Python34/


rowidx=0
roundidx=1

rowmin=0
rowmax=100

arrcustfile=()
fileconnect=""

# Step 5-6: Append the parent files form edward reviewed file list.

edwardcustfile=/h/gitrep/Production/AuthorCustmization/AuthorCustmization/filelist.txt
edwardapppath=/h/gitrep/Production/AuthorCustmization/AuthorCustmization/bin/Release

iappend=0

while read line
do
	reviewfile=`echo "${line}" | cut -d "	" -f 1`
	reviewservice=`echo "${line}" | cut -d "	" -f 2`
	
	echo "${reviewservice}/${reviewfile}" | grep -i "${reviewservice}/${reviewfile}" "${changestatusfile}"
	
	if [ $? -ne 0 ]; then
		# means this file is unsuitable content for mooncake.
		let iappend+=1
		echo -e "${reviewservice}/${reviewfile}\tAPPEND" >> ${changestatusfile}
	fi
	
done <"${edwardcustfile}"

if ([ ${iappend} > 0 ]);then
	echo -e "\n\rAppend ${iappend} rows form edward review change list!"
fi


# Step 6: Go to Jack's application root path and get ready to customize the articles batch by batch.

cd $pythondir

echo -e "\n\rChange to python.exe directory successfully!"

echo -e "\n\rStart to customize the file with Jack's app!"


while read line
do

	filekey=`echo "${line}" | cut -d "	" -f 1`
	filestaus=`echo "${line}" | cut -d "	" -f 2`

	# Step 6.1: We collect the files list for customize later, We only collect for status "APPEND", "NEW" and "UPDATE", discard the "UNSUITABLE" status.
	if ([ "$filestaus" = "NEW" ] || [ "$filestaus" = "UPDATE" ] || [ "$filestaus" = "APPEND" ]);then
		arrcustfile[rowidx]="${filekey}"
		let rowidx+=1
	fi
	
	if [ "$rowidx" -eq "$rowmax" ];then
	
		# we will customize the files with one batch which contains 100.
		echo -e "\n\rCustomize the NO. ${roundidx} batch files collection!"
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
				${winrepodirnodash} H:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				H:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ H:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				H:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ H:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${fileconnect}
		fi
		
		sleep 5
		
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
				${winrepodirnodash} H:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				H:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ H:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				H:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ H:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${fileconnect}
	fi
	
	sleep 5
	
	if true;then
		echo -e "\n\rStep 2: Customize the articles with python app."
		./python.exe ${sourcetreedir}SourceTreeScript.py customize_files ${repodirnodash} ${fileconnect}
	fi
	
fi

cd $repodir

echo -e "\n\r Go to global sync branch - ${branchThisMonth} and wait for 5 second before save stash!"

sleep 5

# Step 7: Save the update files as Jack's customize on branch

git stash save "Jack Customize ${timestamp}"

sleep 5

git stash apply

# Step 8: Save the update files as Jack's customize on branch

sleep 5

cd $edwardapppath

./AuthorCustmization.exe -S F -C A

sleep 5

./AuthorCustmization.exe -S F -C U

sleep 5

./AuthorCustmization.exe -S F -C C

sleep 5

# Step 9: Save the update files as Jack's customize on branch

cd $repodir

echo -e "\n\r Go to global sync branch - ${branchThisMonth} and wait for 5 second before save stash!"

sleep 5

git stash save "Edward Customize ${timestamp}"

git stash apply

echo -e "Customzie the sync files successfully!"