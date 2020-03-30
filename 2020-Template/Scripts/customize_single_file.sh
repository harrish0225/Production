
pythondir=/c/Python34/

cd $pythondir

edwardcustfile=/h/gitrep/Production/AuthorCustmization/AuthorCustmization/filelist.txt
edwardapppath=/h/gitrep/Production/AuthorCustmization/AuthorCustmization/bin/Release

winsourcetreedir="H:\\gitrep\\SourceTreeScript\\SourceTreeScript\\"
winrepodirnodash="H:\\gitrep\\azure-docs-pr"
filefullname=""

while read line
do
	filename=`echo "${line}" | cut -d "	" -f 1`
	filedir=`echo "${line}" | cut -d "	" -f 2`

	filefullname="${filefullname} ${filedir}/${filename}"
	
done <"${edwardcustfile}"

echo "filefullname is ${filefullname}"

./python.exe ${winsourcetreedir}SourceTreeScript.py replace_script \
				${winrepodirnodash} H:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				H:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ H:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				H:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ H:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${filefullname}
				
./python.exe ${winsourcetreedir}SourceTreeScript.py customize_files ${winrepodirnodash} ${filefullname}
	
cd $edwardapppath

./AuthorCustmization.exe -S F -C A

sleep 2

./AuthorCustmization.exe -S F -C U

sleep 2

./AuthorCustmization.exe -S F -C C

sleep 2
