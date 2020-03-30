
basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}"


echo "Select the repository No with following option:"
echo "1. /h/gitrep/azure-docs-pr/"
echo "2. /h/gitrep/mc-docs-pr.en-us/"
echo "3. /m/gitrep/mc-docs-pr.zh-cn.rockboyfor.sxs/"
echo "    "
echo "    "

receiveRepo="no"

while [ ${receiveRepo} == "no" ];
do

	read -p "Enter the repository index digital= " repoIdx

	if [ ${repoIdx} == "1" ]; then
		cd /h/gitrep/azure-docs-pr/
		receiveRepo="yes"
	elif [ ${repoIdx} == "2" ]; then
		cd /h/gitrep/mc-docs-pr.en-us/
		receiveRepo="yes"
	elif [ ${repoIdx} == "3" ]; then
		cd /m/gitrep/mc-docs-pr.zh-cn.rockboyfor.sxs/
		receiveRepo="yes"
	fi
	
done;

echo "    "
echo "    "

timestamp=`date +%Y%m%d%H%M%S`

case "${repoIdx}" in
	"1")
		generatename="${basedir}/ChangeFileListByHead_azure_${timestamp}.txt"
		;;
	"2")
		generatename="${basedir}/ChangeFileListByHead_en-us_${timestamp}.txt"
		;;
	"3")
		generatename="${basedir}/ChangeFileListByHead_zh-cn_${timestamp}.txt"
		;;
esac


git status | cut -d ":" -f 2 | sed -r "s/^ {1,}//g" | sort | uniq >> "${generatename}"

echo "The files of ${generatename} generated successfully!"
