cd /h/gitrep/azure-source-code/azure-docs-cli-python-samples
git pull origin master

cd /h/gitrep/azure-source-code/azure-docs-powershell-samples
git pull origin master

cd /h/gitrep/azure-source-code

dirparents=/h/gitrep/azure-source-code
for directory in $dirparents/*;
do
	cd $directory
	git pull origin master
done