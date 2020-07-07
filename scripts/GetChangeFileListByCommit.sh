#! /bin/bash

basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}"

repodir=/h/gitrep/azure-docs-pr/
# custrepodir=/h/gitrep/mc-docs-pr.en-us/
# sourcetreedir=/h/gitrep/SourceTreeScript/SourceTreeScript/

# Step 1: Go to the global azure-docs-pr repostory and generate file to save the change files list.
cd ${repodir}

if [ -d ${repodir} ];then
	echo -e "\n\rChange to directory : "${repodir}
else
	echo -e "\n\rThe Application directory is not exists  - "${repodir}
	exit -1
fi


# Step 2: Check the hsafile exist.
shalist="${basedir}/hsalist.txt"

if [ -f "${shalist}" ];then
	echo -e "\n\rSearch ${shalist} successfully!"
else
	echo -e "\n\rSearch ${shalist} failed!"
	exit -1
fi

# Step 3: Get the status for the change file list: NEW UPDATE UNSUITABLE.
isFirstRow=true
previousDate=""
previousSHA=""
nextDate=""
nextSHA=""
reportName=""
iIndex=0

git config diff.renamelimit 99999999

while read line
do
	iIndex=`expr $iIndex + 1`
	
	echo "Start to read the No. ${iIndex} row."
	
	if [ true == $isFirstRow ]; then
		echo "current true == isFirstRow"
		
		previousDate=`echo "${line}" | cut -d "	" -f 1`
		previousSHA=`echo "${line}" | cut -d "	" -f 2`
		isFirstRow=false
	else
		echo "current true <> isFirstRow"
		nextDate=`echo "${line}" | cut -d "	" -f 1`
		nextSHA=`echo "${line}" | cut -d "	" -f 2`
		reportName="${basedir}/GetChangeFileListByCommit_${nextDate}.txt"
		
		echo "Processing the commit of ${nextSHA} on ${nextDate}..."
	
		git diff ${previousSHA} ${nextSHA} --diff-filter=A --name-only |\
		grep -E -o "(articles/(aks|analysis\-services|azure\-resource\-manager|container\-registry|cosmos\-db|firewall|private\-link|\
		service\-fabric|service\-fabric|site\-recovery|sql\-server\-stretch\-database|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan|\
		active\-directory|active\-directory\-b2c|advisor|api\-management|application\-gateway|app\-service|app\-service\-mobile|automation|\
		azure\-cache\-for\-redis|azure\-functions|azure\-monitor|azure\-portal|azure\-stack|backup|batch|cloud\-services|cognitive\-services|\
		connectors|container\-instances|data\-explorer|data\-factory|dms|documentdb|event\-grid|expressroute|governace|guides|hdinsight|iot\-dps|\
		iot\-edge|iot\-hub|key\-vault|load\-balancer|logic\-apps|mariadb|media|media\-servcies|multi\-factor\-authentication|mysql|networking|\
		network\-watcher|notification\-hubs|postgresql|power\-bi\-embedded|power\-bi\-workspace\-collections|resiliency|role\-base\-access\-control|\
		schedule|security|security\-center|servcie\-bus|service\-health|sql\-database|sql\-data\-warehouse|sql\-server\-stretch\-database|storage|\
		stream\-analytics|time\-series\-insigihts|virtual\-machines\-scale\-sets|vpn\-gateway\
		)/|includes/)\S*\.(md|yml)" |\
		sort | uniq |\
		sed -r "s/(\.md|\.yml)/\1\tNEW/g" >> ${reportName}
		
		git diff ${previousSHA} ${nextSHA} --diff-filter=M --name-only |\
		grep -E -o "(articles/(aks|analysis\-services|azure\-resource\-manager|container\-registry|cosmos\-db|firewall|private\-link|\
		service\-fabric|service\-fabric|site\-recovery|sql\-server\-stretch\-database|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan|\
		active\-directory|active\-directory\-b2c|advisor|api\-management|application\-gateway|app\-service|app\-service\-mobile|automation|\
		azure\-cache\-for\-redis|azure\-functions|azure\-monitor|azure\-portal|azure\-stack|backup|batch|cloud\-services|cognitive\-services|\
		connectors|container\-instances|data\-explorer|data\-factory|dms|documentdb|event\-grid|expressroute|governace|guides|hdinsight|iot\-dps|\
		iot\-edge|iot\-hub|key\-vault|load\-balancer|logic\-apps|mariadb|media|media\-servcies|multi\-factor\-authentication|mysql|networking|\
		network\-watcher|notification\-hubs|postgresql|power\-bi\-embedded|power\-bi\-workspace\-collections|resiliency|role\-base\-access\-control|\
		schedule|security|security\-center|servcie\-bus|service\-health|sql\-database|sql\-data\-warehouse|sql\-server\-stretch\-database|storage|\
		stream\-analytics|time\-series\-insigihts|virtual\-machines\-scale\-sets|vpn\-gateway\
		)/|includes/)\S*\.(md|yml)" |\
		sort | uniq |\
		sed -r "s/(\.md|\.yml)/\1\tUPDATE/g" >> ${reportName}
		
		git diff ${previousSHA} ${nextSHA} --diff-filter=D --name-only |\
		grep -E -o "(articles/(aks|analysis\-services|azure\-resource\-manager|container\-registry|cosmos\-db|firewall|private\-link|\
		service\-fabric|service\-fabric|site\-recovery|sql\-server\-stretch\-database|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan|\
		active\-directory|active\-directory\-b2c|advisor|api\-management|application\-gateway|app\-service|app\-service\-mobile|automation|\
		azure\-cache\-for\-redis|azure\-functions|azure\-monitor|azure\-portal|azure\-stack|backup|batch|cloud\-services|cognitive\-services|\
		connectors|container\-instances|data\-explorer|data\-factory|dms|documentdb|event\-grid|expressroute|governace|guides|hdinsight|iot\-dps|\
		iot\-edge|iot\-hub|key\-vault|load\-balancer|logic\-apps|mariadb|media|media\-servcies|multi\-factor\-authentication|mysql|networking|\
		network\-watcher|notification\-hubs|postgresql|power\-bi\-embedded|power\-bi\-workspace\-collections|resiliency|role\-base\-access\-control|\
		schedule|security|security\-center|servcie\-bus|service\-health|sql\-database|sql\-data\-warehouse|sql\-server\-stretch\-database|storage|\
		stream\-analytics|time\-series\-insigihts|virtual\-machines\-scale\-sets|vpn\-gateway\
		)/|includes/)\S*\.(md|yml)" |\
		sort | uniq |\
		sed -r "s/(\.md|\.yml)/\1\tDELETE/g" >> ${reportName}
		
		git diff ${previousSHA} ${nextSHA} --diff-filter=R --name-only |\
		grep -E -o "(articles/(aks|analysis\-services|azure\-resource\-manager|container\-registry|cosmos\-db|firewall|private\-link|\
		service\-fabric|service\-fabric|site\-recovery|sql\-server\-stretch\-database|traffic\-manager|virtual\-machines|virtual\-network|virtual\-wan|\
		active\-directory|active\-directory\-b2c|advisor|api\-management|application\-gateway|app\-service|app\-service\-mobile|automation|\
		azure\-cache\-for\-redis|azure\-functions|azure\-monitor|azure\-portal|azure\-stack|backup|batch|cloud\-services|cognitive\-services|\
		connectors|container\-instances|data\-explorer|data\-factory|dms|documentdb|event\-grid|expressroute|governace|guides|hdinsight|iot\-dps|\
		iot\-edge|iot\-hub|key\-vault|load\-balancer|logic\-apps|mariadb|media|media\-servcies|multi\-factor\-authentication|mysql|networking|\
		network\-watcher|notification\-hubs|postgresql|power\-bi\-embedded|power\-bi\-workspace\-collections|resiliency|role\-base\-access\-control|\
		schedule|security|security\-center|servcie\-bus|service\-health|sql\-database|sql\-data\-warehouse|sql\-server\-stretch\-database|storage|\
		stream\-analytics|time\-series\-insigihts|virtual\-machines\-scale\-sets|vpn\-gateway\
		)/|includes/)\S*\.(md|yml)" |\
		sort | uniq |\
		sed -r "s/(\.md|\.yml)/\1\tRENAME/g" >> ${reportName}
		
		echo "Generate the report named ${reportName} successfully!"
		echo ""
		
		previousDate=${nextDate}
		previousSHA=${nextSHA}
	fi
	
	
done < "${shalist}"

