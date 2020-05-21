
pythondir=/c/Python34/

cd $pythondir

edwardcustfile=/d/gitrep/Production/AuthorCustomization/AuthorCustomization/filelist.txt
edwardapppath=/d/gitrep/Production/AuthorCustomization/AuthorCustomization/bin/Release

winsourcetreedir="D:\\gitrep\\SourceTreeScript\\SourceTreeScript\\"
winrepodirnodash="D:\\gitrep\\azure-docs-pr"
filefullname=""

while read line
do
	filename=`echo "${line}" | cut -d "	" -f 1`
	filedir=`echo "${line}" | cut -d "	" -f 2`

	filefullname="${filefullname} ${filedir}/${filename}"
	
done <"${edwardcustfile}"

echo "filefullname is ${filefullname}"

./python.exe ${winsourcetreedir}SourceTreeScript.py replace_script \
				${winrepodirnodash} D:\\gitrep\\azure-source-code\\azure-docs-cli-python-samples\\ \
				D:\\gitrep\\azure-source-code\\azure-docs-powershell-samples\\ D:\\gitrep\\azure-source-code\\azure-quickstart-templates\\ \
				D:\\gitrep\\azure-source-code\\cosmos-dotnet-todo-app\\ D:\\gitrep\\azure-source-code\\azure-cosmos-db-sql-api-nodejs-getting-started\\ \
				${filefullname}
				
./python.exe ${winsourcetreedir}SourceTreeScript.py customize_files ${winrepodirnodash} ${filefullname}
	
cd $edwardapppath

./AuthorCustomization.exe -S F -C A

sleep 2

./AuthorCustomization.exe -S F -C U

sleep 2

./AuthorCustomization.exe -S F -C C

sleep 2
