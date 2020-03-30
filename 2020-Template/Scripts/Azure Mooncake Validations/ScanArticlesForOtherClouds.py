#!/usr/local/bin/python
from sys import argv
import logging

class ScanningRules:
    def __init__(self):
        self.rules = []
    
    def addRule(self, category, target, replacement, message):
        rule = (category, target, replacement, message)
        self.rules.append(rule)
    
    def addRule(self, t):
        self.rules.append(t)

    def addRulesFromFile(self,fname):
        with open(fname) as f:
            lines = f.read().splitlines()

            for line in lines:
                t = tuple(filter(None, line.split(';')))
                self.addRule(t)

def getListOfArticles(fname):

    with open(fname) as f:
        temp = f.read().splitlines()

        lines = []
        for line in temp:
            lines.append(line.replace("/", "\\"))

        return lines

def mooncakeScanLine(line, lineNum, scanningRules, logger):
    for rule in scanningRules.rules:
        if(line.find(rule[1]) !=-1):
            logger.info("\tline number: {0}.  Instance of \"{1}\" found.  Replace with \"{2}\". {3}".format(lineNum, rule[1], rule[2], rule[3]))    
    
def scan(article, function, rulesFile, logger):

    # Get scanning rules
    scanningRules = ScanningRules()
    
    scanningRules.addRulesFromFile(rulesFile)

    logger.info("Scanning " + article)
    with open(article, 'r', encoding='utf-8') as file:              # create a file object    
        lines = file.read().splitlines()          # call file methods
        for i, line in enumerate(lines, start=1):               # until end-of-file
            function(line, i, scanningRules, logger)                   # call a function object
    

def main():

    #Create and configure logger 
    logFile = "log.txt"    
    logging.basicConfig(filename=logFile, 
                    format='%(message)s', 
                    filemode='w') 
  
    #Creating an object 
    logger=logging.getLogger() 
    
    #Setting the threshold of logger to DEBUG 
    logger.setLevel(logging.DEBUG) 

    basePath = "C:\\Users\\ryanwi\\azure-docs-pr\\articles\\"
    articles = getListOfArticles("ArticlesToScan.txt")

    # Scanning rules
    rulesFile="MooncakeRules.txt"

    # Scan through list of articles
    for article in articles:
        scan(basePath+article, mooncakeScanLine, rulesFile, logger)

if __name__ == "__main__":
    main()