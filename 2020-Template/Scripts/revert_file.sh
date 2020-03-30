#! /bin/bash

basedir=$(cd `dirname $0`; pwd)
echo -e "\n\rCurrent application path is ${basedir}\n\r"

read -p "Provider SN txt file Name to convert sequence: serialnofile=" serialnofile

currentfile="${basedir}/${serialnofile}.txt"


if [ -f "${currentfile}" ];then
	echo -e "\n\rSearch ${currentfile} successfully!"
else
	echo -e "\n\rPlease check the Seiral No file eixsts or not on current path!\n\r"
    echo -e "\n\rFileName!${currentfile}\n\r"
	exit -1
fi

timestamp=`date +%Y%m%d%H%M$S`
generatename="${basedir}/${serialnofile}_convert_${timestamp}.txt"


echo "" >> "${generatename}"

oddContent=""

echo -e "The application is running now, please wait for closing itself...\n"

while read line
do
    oddContent="${line}\n${oddContent}"
done <"${currentfile}"

echo -e ${oddContent} >> "${generatename}"

echo "The application is flushing content to convert file now, please wait for closing itself...\n"

sleep 10

echo -e "\n\rCurrent the revert file was generated successfully on \n\r${generatename}\n\r"

echo -e "You can close the application yourslef now!"
