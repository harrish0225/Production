
pythondir=/c/Python34/

cd $pythondir

edwardcustfile=/h/gitrep/Production/AuthorCustomization/AuthorCustomization/filelist.txt

winsourcetreedir="D:\\gitrep\\SourceTreeScript\\SourceTreeScript\\"
winrepodirnodash="D:\\gitrep\\mc-docs-pr.en-us\\"
filefullname=""

while read line
do
	filename=`echo "${line}" | cut -d "	" -f 1`
	filedir=`echo "${line}" | cut -d "	" -f 2`

	filefullname="${filefullname} ${filedir}/${filename}"
	
done <"${edwardcustfile}"

echo "filefullname is ${filefullname}"

./python.exe ${winsourcetreedir}SourceTreeScript.py pantool ${winrepodirnodash} ${filefullname}

