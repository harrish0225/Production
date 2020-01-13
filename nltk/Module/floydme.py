#!/usr/bin/env python
# coding=utf-8

import sys

class floyd:
    """The Floyd implementation"""
    # https://www.xuebuyuan.com/3238631.html
    __inf = 999999999999
    __zero = 0
    __nodeCount = 0
    __FactorList = []
    __FactorPath = []
    __userChoice = 0
    __initValue = 0
    __calc = False
    
    
    def getNodeCount(self):
        return self.__nodeCount
        
    def setNodeCount(self, value):
        self.__nodeCount = int(value)
        
    def getUserChoice(self):
        return self.__userChoice
    
    def getCalc(self):
        return self.__calc
        
    def setCalc(self, value):
        self.__calc = value
        
    def getInf(self):
        return self.__inf
        
    def getZero(self):
        return self.__zero
        
    def setUserChoice(self, value):
        self.__userChoice = value
    
    def getInitValue(self):
        return self.__initValue
        
    def setInitValue(self, value):
        self.__initValue = value
        
    def getFactorList(self):
        return self.__FactorList
        
    def setFactorList(self, processList):
        self.__FactorList = processList
        
    def getFactorPath(self):
        return self.__FactorPath
        
    def setFactorPath(self, processList):
        self.__FactorPath = processList
    
    def __init__(self, nodecount):
        
        while(not(str(nodecount).isdigit() == True and int(nodecount) > 1)):
            
            print("Floyd's nodeCount should be integer and greater than 0.\n")
            nodecount=input('Please reset the node count of the Floyd class in advanced!\n')
            
        self.setNodeCount(nodecount)
        
        factor = self.getFactorList()
        path = self.getFactorPath()
        
        initInf = self.getInf()
        initZero = self.getZero()
        
        initCount = self.getNodeCount()
        
        factor = [[initInf for i in range(initCount)] for j in range(initCount)]
        
        path = [[initZero for i in range(initCount)] for j in range(initCount)]
        
        for i in range(initCount):
            for j in range(initCount):
                if(i==j):
                    factor[i][j] = initZero
                    
                path[i][j] = j
        
        self.setFactorList(factor)
        self.setFactorPath(path)
        
        print("Initilized the Floyd's Matric List completed!\n")
        
    def ConfigFactor(self):
        
        nodecount = self.getNodeCount()
        
        factor = self.getFactorList()
        
        noteMsg = 'Please type the factor value list for Floyd class!\nFormat:[[rowIdx,colIdx,factor],]\nAll the elements will be integer.\nType quit to exit the initilization process!\n\n'
        noteMsgInWhile = 'Paste the List to configur the factor or type quit to exit this process!\n'
        curtList = input(noteMsg)
        
        while(curtList.lower()!="quit"):
            
            try:
                convertList = eval(curtList)
                isDigit = True
                isCheckAll = True
                for item in convertList:
                    if(str(item[0]).isdigit() == False or str(item[1]).isdigit() == False or str(item[2]).isdigit() == False):
                        isDigit = False
                        break
                 
                if (isDigit == False):
                    isCheckAll = False
                else:
                    maxDimens = max([max(i,j) for (i,j,k) in convertList])
                    if(maxDimens>nodecount+1):
                        isCheckAll = False
                        
                
                if(isCheckAll == True):
                    for item in convertList:
                        factor[item[0]][item[1]] = item[2]
                    self.setFactorList(factor)
                
                curtList = input(noteMsgInWhile)
                
            except Exception as e:
                curtList = input(noteMsgInWhile)
                
        
        
    def AddPadChar(self,custstr, width, padChar):
        
        customstr = custstr
        for i in range(width-len(custstr)):
            customstr = padChar + customstr
            
        return customstr
    
    def PrintFactorPathMatrix(self):
        
        path = self.getFactorPath()
        
        charLen = max(len(str(cell))+1 for row in path for cell in row)
        
        nodecount = self.getNodeCount()
        
        titlecount = len(str(nodecount))+1 if len(str(nodecount)) + 1 > 5 else 5
        
        if (charLen<titlecount):
            charLen=titlecount
        
        # print the matrix title
        print("-"*10 + "Begin of Print Factor Path" + "-"*10)
        print("INDEX".center(charLen),end="|")
        
        for rowidx in range(nodecount):
            customstr = "R" + self.AddPadChar(str(rowidx+1),charLen - len("R"),"0")
            print(customstr.center(charLen),end="|")
            
        print()
        # print the matrix content
        for rowidx in range(nodecount):
            customstr = "C" + self.AddPadChar(str(rowidx+1),charLen - len("C"),"0")
            print(customstr.center(charLen),end="|")
            
            for colidx in range(nodecount):
                customstr = str(path[rowidx][colidx])
                print(customstr.rjust(charLen),end="|")
                
            print()
            
        print("-"*10 + "End of Print Factor Path" + "-"*10)
         
    def PrintFactorMatrix(self):
        
        factor = self.getFactorList()
        
        charLen = max(len(str(cell))+1 for row in factor for cell in row)
        
        nodecount = self.getNodeCount()
        
        titlecount = len(str(nodecount))+1 if len(str(nodecount)) + 1 > 5 else 5
        
        if (charLen<titlecount):
            charLen=titlecount
        
        print("-"*10 + "Begin of Print Factor List" + "-"*10)
        # print the matrix title
        print("INDEX".center(charLen),end="|")
        
        for rowidx in range(nodecount):
            customstr = "R" + self.AddPadChar(str(rowidx+1),charLen - len("R"),"0")
            print(customstr.center(charLen),end="|")
            
        print()
        # print the matrix content
        for rowidx in range(nodecount):
            customstr = "C" + self.AddPadChar(str(rowidx+1),charLen - len("C"),"0")
            print(customstr.center(charLen),end="|")
            
            for colidx in range(nodecount):
                customstr = str(factor[rowidx][colidx])
                print(customstr.rjust(charLen),end="|")
                
            print()
        print("-"*10 + "End of Print Factor List" + "-"*10)
    
    def PrintPath(self):
    
        if(self.getCalc==False):
            self.Calculate()
            
        nodecount = self.getNodeCount()
        
        factor = self.getFactorList()
        path = self.getFactorPath()
        
        for i in range(nodecount):
            for j in range(i+1,nodecount):
                print("V%d-->V%d weight: %d Path: %d " % (i,j,factor[i][j],i), end=" ")
                k = path[i][j]
                while(k != j):
                    print(" --> %d" % k, end=" ")
                    k = path[k][j]
                print(" --> %d" % j)
            print()
            
        
    def Calculate(self):
        # Floyd-Warshall core 
        factor = self.getFactorList()
        path = self.getFactorPath()
        nodecount = self.getNodeCount()
        initInf = self.getInf()
        
        print("Begin calculate the optinal route with Floyd-Warshall core!")
        
        for k in range(nodecount):
            for i in range(nodecount):
                for j in range(nodecount):
                    if(factor[i][k] < initInf and factor[k][j] < initInf ):
                        if(factor[i][j] > factor[i][k] + factor[k][j]):
                            factor[i][j] = factor[i][k] + factor[k][j]
                            path[i][j] = path[i][k]
                            
        self.setCalc(True)
        
        print("Calculate the optinal route complete!")

                    
                      
if(__name__=="__main__"):
    sys.path.append('D:\gitrep\Production\nltk\Module')
    
# my = Floyd(9)
#[[0,1,1],[0,2,5],[1,0,1],[1,2,3],[1,3,7],[1,4,5],[2,0,5],[2,1,3],[2,4,1],[2,5,7],[3,1,7],[3,4,2],[3,6,3],[4,1,5],[4,2,1],[4,3,2],[4,5,3],[4,6,6],[4,7,9],[5,2,7],[5,4,3],[5,7,5],[6,3,3],[6,4,6],[6,7,2],[6,8,7],[7,4,9],[7,5,5],[7,6,2],[7,8,4],[8,6,7],[8,7,4]]


