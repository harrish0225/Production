cd /h/gitrep/azure-source-code/azure-docs-cli-python-samples
git checkout .		# discard all the updates file content which not includes all NEW files.
git pull origin master

cd /h/gitrep/azure-source-code/azure-docs-powershell-samples
git checkout .
git pull origin master

cd /h/gitrep/azure-source-code

dirparents=/h/gitrep/azure-source-code
for directory in $dirparents/*;
do
	cd $directory
	git checkout .
	git pull origin master
done