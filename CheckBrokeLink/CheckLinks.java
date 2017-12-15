package wacn;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import javax.net.ssl.SSLHandshakeException;

import org.apache.commons.lang3.StringUtils;


public class CheckLinks {
	
	public static void main(String[] args) {
        startCheck("E:\\my_files\\mooncake\\mc-docs-pr.zh-cn\\articles\\key-vault", false);
        
        
        
        
    }
	
	private static void startCheck(String folderName, boolean printAllLinks) {
        List<String> fileList = new ArrayList<String>(200);
        
        readFileNameFromSourceFolder(folderName, fileList);        		
        		
        int count = 1;
        for(String file: fileList) {
        	System.out.println(count++ + ". " + file.substring(file.lastIndexOf("\\") + 1));  
        	
        	checkAllLinks(file, printAllLinks);
        	
        	System.out.println(); 
        }
    }
	
	public static void readFileNameFromSourceFolder(String folderName, List<String> fileList) {
		File sourceFolder = new File(folderName);

		for (File file : sourceFolder.listFiles()) {
			if (file.isFile() && file.getName().endsWith(".md")) {
				fileList.add(file.getAbsolutePath());
			} else if(file.isDirectory()) {
				readFileNameFromSourceFolder(file.getAbsolutePath(), fileList);
			}
		}
	}

	public static synchronized void checkAllLinks(String fileName, boolean printAllLinks) {
		System.out.println("checking links. please wait...");

		List<String> allLinkList = getAllLinks(fileName);

		BufferedReader reader = null;
		for (String link : allLinkList) {
			link = link.trim();

			if (link.startsWith("https://manage.windowsazure.cn") || link.startsWith("http://manage.windowsazure.cn")) {
				continue;
			}

			if (link.startsWith("www")) {
				link = "http://" + link;
			}

			int respCode = -1;

			try {
				URL url = new URL(link);
				HttpURLConnection conn = (HttpURLConnection) url.openConnection();
				conn.setRequestMethod("GET");
				conn.setDoInput(true);
				conn.setDoOutput(true);

				respCode = conn.getResponseCode();

				StringBuilder sb = new StringBuilder();

				reader = new BufferedReader(new InputStreamReader(conn.getInputStream(), "UTF-8"));
				String str = null;
				while ((str = reader.readLine()) != null) {
					sb.append(str);
				}

				if(printAllLinks) {
					System.out.println(link);
				}
				
				String content = sb.toString();

				if (respCode == 404 || content.toLowerCase().contains("errors_404")) {
					System.err.println("404 error: --> " + link);
				}

				if (respCode == 502) {
					System.err.println("502 error: --> " + link);
				}

				if (content.contains("未能找到您请求的页面")) {
					System.err.println("未能找到您请求的页面，请检查: --> " + link);
				}

				if (content.contains("已移至") || content.contains("This topic has been move")) {
					System.err.println("该主题已迁移或不可用，请检查: --> " + link);
				}
			} catch (SSLHandshakeException se) {
				System.err.println("SSLHandshakeException: --> " + link);
			} catch (java.net.ConnectException se) {
				System.err.println("ConnectException: --> " + link);
			} catch (FileNotFoundException fe) {
				System.err.println("无用链接: --> " + link);

			} catch (UnknownHostException ue) {
				System.err.println("无用链接: --> " + link);
			} catch (Exception e) {
				if (respCode != 403) {
					System.out.println(link);

					e.printStackTrace();
				}
			} finally {
				try {
					if (reader != null) {
						reader.close();
						reader = null;
					}
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		}

		System.out.println("checking links done....");
	}

	private static List<String> getAllLinks(String fileName) {
		List<String> allLinks = new ArrayList<String>();

		if (fileName == null || fileName.equals("")) {
			return allLinks;
		}

		BufferedReader reader = null;

		try {
			reader = new BufferedReader(new InputStreamReader(new FileInputStream(fileName), "utf-8"));

			String line = null;
			while ((line = reader.readLine()) != null) {
				List<String> link1 = getLineLinks(fileName, line);

				List<String> link2 = getLineLinks_2(fileName, line);

				allLinks.addAll(link1);

				allLinks.addAll(link2);
			}

		} catch (FileNotFoundException e) {
			e.printStackTrace();
		} catch (IOException e) {
			e.printStackTrace();
		} finally {
			try {
				if (reader != null) {
					reader.close();
					reader = null;
				}
			} catch (IOException e) {
				e.printStackTrace();
			}
		}

		return allLinks;
	}
	
	private static List<String> getLineLinks(String fileName, String line) {
		List<String> urlList = new ArrayList<String>();

		String patternStr = "(\\[[^\\[]+\\]\\([^\\(\\[]+\\))+";
		Pattern pattern = Pattern.compile(patternStr);

		Matcher matcher = pattern.matcher(line);
		while (matcher.find()) {
			String matchedString = matcher.group();

			String subStr = matchedString.substring(matchedString.lastIndexOf("]"));

			String url = subStr.substring(subStr.indexOf("(") + 1, subStr.indexOf(")")).trim();

			if (url.startsWith("/")) {

				urlList.add("https://docs.azure.cn" + url);
			} else if (url.contains("http:") || url.contains("www.") || url.contains("https:")) {

				urlList.add(url);
			} else if (url.startsWith("../") && url.contains(".md") && !url.contains("../includes/")) {
				String relativePath = getRelativeFilePath(fileName);

				int numOfPointPointSlash = StringUtils.countMatches(url, "../");
				int numOfSlash = StringUtils.countMatches(relativePath, "/"); // eg:
																				// /batch/scripts
				if (numOfPointPointSlash > numOfSlash) {
					System.err.println("Wrong url: --> " + url);
					continue;
				}

				String newRelativePath = relativePath;
				for (int i = 0; i < numOfPointPointSlash; i++) {
					newRelativePath = newRelativePath.substring(0, newRelativePath.lastIndexOf("/"));
				}

				if (!newRelativePath.equals("") && !newRelativePath.startsWith("/")) {
					newRelativePath = "/" + newRelativePath;
				}

				urlList.add("https://docs.azure.cn/zh-cn" + newRelativePath + "/" + url.replace("../",   "").replaceAll(".md",   ""));

			} else if (url.startsWith("./") && url.contains(".md") && !url.contains("../includes/")) {

				urlList.add("https://docs.azure.cn/zh-cn" + getRelativeFilePath(fileName) + "/" + url.replace("./",   "").replace(".md",   ""));
			} else if (url.contains(".md") && !url.contains("../includes/")) {

				urlList.add("https://docs.azure.cn/zh-cn" + getRelativeFilePath(fileName) + "/" + url.replace(".md",   ""));
			} else if (url.startsWith("#") || url.startsWith("./media") || url.startsWith("media") || url.contains("../includes/")) {

			} else {
				System.err.println("Wrong url: --> " + url);
				continue;
			}
		}

		return urlList;
	}
	
	private static List<String> getLineLinks_2(String fileName, String line) {
		List<String> urlList = new ArrayList<String>();

		String patternStr = "(\\[[^\\[]+\\]):.+";
		Pattern pattern = Pattern.compile(patternStr);

		Matcher matcher = pattern.matcher(line);
		if (matcher.find()) {
			String matchedString = matcher.group();

			String subStr = matchedString.substring(matchedString.indexOf("]"));

			String url = subStr.substring(subStr.indexOf(":") + 1).trim();

			if (url.startsWith("/")) {
				urlList.add("https://docs.azure.cn" + url);
			} else if (url.contains("http:") || url.contains("www.") || url.contains("https:")) {
				urlList.add(url);
			} else if (url.startsWith("../") && url.contains(".md") && !url.contains("../includes/")) {
				String relativePath = getRelativeFilePath(fileName);

				int numOfPointPointSlash = StringUtils.countMatches(url, "../");
				int numOfSlash = StringUtils.countMatches(relativePath, "/"); // eg: /batch/scripts

				if (numOfPointPointSlash > numOfSlash) {
					System.err.println("Wrong url: --> " + url);
					return urlList;
				}

				String newRelativePath = relativePath;
				for (int i = 0; i < numOfPointPointSlash; i++) {
					newRelativePath = newRelativePath.substring(0, newRelativePath.lastIndexOf("/"));
				}

				if (!newRelativePath.equals("") && !newRelativePath.startsWith("/")) {
					newRelativePath = "/" + newRelativePath;
				}

				urlList.add("https://docs.azure.cn/zh-cn" + newRelativePath + "/" + url.replace("../",   "").replaceAll(".md",   ""));

			} else if (url.startsWith("./") && url.contains(".md") && !url.contains("../includes/")) {

				urlList.add("https://docs.azure.cn/zh-cn" + getRelativeFilePath(fileName) + "/" + url.replace("./",   "").replace(".md",   ""));
			} else if (url.contains(".md") && !url.contains("../includes/")) {

				urlList.add("https://docs.azure.cn/zh-cn" + getRelativeFilePath(fileName) + "/" + url.replace(".md",   ""));
			} else if (url.startsWith("#") || url.startsWith("./media") || url.startsWith("media") || url.contains("../includes/")) {

			} else {
				System.err.println("Wrong url: --> " + url);
			}
		}

		return urlList;
	}

	private static String getRelativeFilePath(String fileName) {
		String path = fileName.substring(fileName.indexOf("articles")).replace("\\",   "/");

		path = path.substring(path.indexOf("/"), path.lastIndexOf("/")); 

		return path;
	}
}
