#! /bin/bash

basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}\n\r"

read -p "Provider SN txt file Name to convert sequence: MaxSerialNo=" MaxSerialNo

currentfile="${basedir}/${MaxSerialNo}.txt"

echo "" >> "${currentfile}"

if [ -f "${currentfile}" ];then
	echo -e "\n\rGenerate the file ${currentfile} successfully!"
else
	echo -e "\n\rPlease check the Seiral No file eixsts or not on current path!\n\r"
    echo -e "\n\rFileName!${currentfile}\n\r"
	exit -1
fi

curRow=1
oddContent=""

while [ $curRow -le $MaxSerialNo ]
do
    echo $curRow
    oddContent="${oddContent}\n${curRow}"
    let curRow++
    
done


echo -e "${oddContent}" >> "${currentfile}"


echo -e "\n\rPlease wait for the application close and exit with himeself!\n${currentfile}\n"
