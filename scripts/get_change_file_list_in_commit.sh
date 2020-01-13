
basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}"

echo "Select the repository No with following option:"
echo "1. /d/gitrep/mc-docs-pr.en-us/"
echo "2. /f/gitrep/mc-docs-pr.zh-cn.rockboyfor.sxs/"
echo "    "
echo "    "

receiveRepo="no"

while [ ${receiveRepo} == "no" ];
do

	read -p "Enter the repository index digital= " repoIdx

	if [ ${repoIdx} == "1" ]; then
		cd /d/gitrep/mc-docs-pr.en-us/
		receiveRepo="yes"
	elif [ ${repoIdx} == "2" ]; then
		cd /f/gitrep/mc-docs-pr.zh-cn.rockboyfor.sxs/
		receiveRepo="yes"
	fi
	
done;

timestamp=`date +%Y%m%d%H%M%S`
case "${repoIdx}" in
	"1")
		generatename="${basedir}/ChangeFileListinByCommit_en-us_${timestamp}.txt"
		;;
	"2")
		generatename="${basedir}/ChangeFileListinByCommit_zh-cn_sxs_${timestamp}.txt"
		;;
esac

read -p "Provider the SHA of commitprevious= " commitprevious
read -p "Provider the SHA of commityousubmit= " commityousubmit

git diff $commitprevious $commityousubmit --numstat | cut -f 2,3 | sed -r "s/^0/New/g" | sed -r "s/^[0-9]{1,}/Update/g" | sort | uniq >> "${generatename}"

echo "Generate the change files list on ${generatename}!"
