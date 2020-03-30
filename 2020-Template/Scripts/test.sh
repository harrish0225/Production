cd /d/gitrep/azure-source-code/azure-docs-cli-python-samples
git pull origin master

cd /d/gitrep/azure-source-code/azure-docs-powershell-samples
git pull origin master

cd /d/gitrep/azure-source-code

dirparents=/d/gitrep/azure-source-code
for directory in $dirparents/*;
do
	cd $directory
	git pull origin master
done