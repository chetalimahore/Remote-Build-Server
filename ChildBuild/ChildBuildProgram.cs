///////////////////////////////////////////////////////////////////////
// ChildBuildProgram.cs -    Receives the build request from the mother builder. It parses the build request //
//                          and asks the repository to transfer the files which is stored in child's local storage.
//                          The child builder attempts to build the build request and if successful, creates library files i.e. .dll file 
//                          Then, it sends the test request and the libraries to the test harness for execution.
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
 * The ChildBuild is created by the mother builder. It receives the build request from the mother builder and parses it.
 * After parsing the build request, it asks the repository to transfer the files from repository's storage to its own local storage.
 * The child builder attempts to build the XML build request which may have multiple tests in it. If the tests are successfully built, 
 * library files in the form of .dll are created. A test request with the .dll files is created for every successful test block.
 * The test request and the library files are then transferred to the test harness storage for execution.
 * 
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.Threading;
 *   - System.IO;
 *   
 *  Package Operations:
 *  -------------------
 * This package defines two classes:
 * 
 *   ChildBuildProgram which implements the public methods:
 *   ----------------------------------------------------------
 *   - createChildProcess   : it creates its own receiver and sender objects for communicating with mother and the repository.
 *   - receiveContinuous    : thread which receives messages continuously from mother and repository
 *   - acknowledgeFirst     : receives the reply message from repository for its request message at the initial stage
 *   - sendXmlName          : sends the xml file name to repository in order to fetch the file from repository
 *   - sendParsedFilesName  : sends the names of the dependent files after parsing the build request
 *   - invokeParse          : after receiving the xml file from repository, it parses it by calling call_parse function
 *   - parseForFiles        : it creates an object of the Parse_TestRequest class and parses the file and saves the dependent files
 *   - sendReadyMsgToMother : sends back the ready message to mother when it finishes the execution
 *   - loadXml              : loads the XML at the specific path
 *   - createDllAndTransfer : function for calling the createDll method and transferring build logs to repo
 *   - fileTransferToRepo   : transfers the build logs to repository
 *   - fileTransferToTest   : transfers the test request and the .dll files to test harness
 *   - getFiles             : fetches the files of specific pattern
 *   - getFilesHandler      : finds the files in the specified directory and subdirectory and adds it to the list
 *   - copyFiles            : copies the file from child's main storage to child's temporary storage for creating .dll file
 *   - callParse            : parses the build request for the tests and saves each test's testdriver and testfiles in the array of tests
 *   - createDllFile        : attempts to build the .dll file and saves the build results in the log file
 *   - cleanPath            : cleans the specified pattern of files from the given path
 *   - createTestRequest    : creates the test request with the .dll files
 *   - quit                 : closes all the sender objects
 * 
 *  
 *   Parse_TestRequest which implements the public methods:
 *   -----------------------------------------------------------
 *   - loadXml          : loads the XML from the specified path
 *   - parse_xml        : parses the request, calls parseList method and returns the dependent files
 *   - parseList        : parses the build request for tested propertyName
 * 
 * 
 * The Package also implements the class MPCommService with public methods:
 *  ------------------------------------------------------------
 *  
 *  - postmessage           : sender posts the message on the channel for the receiver to get the message
 *  - getmessage            : receiver gets the message from the channel
 *  - postFile              : transfers the files
 *  
 * 
 * Required Files:
 * ---------------
 * ChildBuildProgram.cs
 * MPCommService.cs
 * IMPCommService.cs
 * TestUtilities.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.2 : 6th December 2017
 * - second release: 
 *      -added functionalities like creating .dll file for multiple tests and executing them in Test Harness
 *      -Sending build and test logs to repository and displaying them on GUI
 * 
 * ver 1.1 : 27th October 2017
 * - added public documentation and added prologues
 * 
 * ver 1.0 : 26th October 2017
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace ChildBuild
{
    /*---------------------------<creates the child processes, parses the request and communicates with the mother and repository>---------*/

    public class ChildBuildProgram
    {
        private string local_address { get; set; } = "http://localhost";
        private static string savePath { get; set; } = "../../../ChildBuild/ChildStorage/";
        private static string mainPath { get; set; } = "../../../ChildBuild/ChildStorage/";
        private static string temppath { get; set; } = null;
        private static string testpath { get; set; } = "../../../TestHarness/TestHarnessStorage/";
        private static string repopath_logs { get; set; } = "../../../Repository/RepoStorage/Generated_Logs/";
        private Receiver receiver = new Receiver();
        private Sender s = null;
        private Sender send_repo = null;
        private Sender send_test = null;
        private int port = 0;
        //private int main_port = 8080;
        private bool flag = true;
        private XDocument doc { get; set; } = null;
        private static List<string> files1 { get; set; } = new List<string>();

        /*-------<creates the child process, starts its own receiver and sender objects communicating to mother and repository>-------*/

        public void createChildProcess(string[] args)
        {
            Console.Title = "ChildBuildProgram";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n  Demo Child Process");
            Console.Write("\n ====================");
            port = Int32.Parse(args[1]);
            Console.Write("\n  Hello from child #{0}\n", args[0]);
            Console.WriteLine("The child is running on port " + args[1]);
            try
            {
                receiver.start(local_address, port);
            }
            catch(Exception e)
            {
                string ex = e.Message.ToString();
                Console.WriteLine("Process is already running on the port \n");
                Console.WriteLine("Close the process first");
            }
        
            s = new Sender(local_address, 8080);
            send_repo = new Sender(local_address, 8079);
            send_test = new Sender(local_address, 8500);
        }

        /*-------------------<sends back the ready message to mother when it finishes the execution>---------------------*/

        public void sendReadyMsgToMother(string[] args)
        {
            CommMessage cm = new CommMessage(CommMessage.MessageType.reply);
            cm.command = "Ready";
            cm.author = "Chetali Mahore";
            cm.to = "http://localhost:8080/IPluggableComm";
            cm.from = "Child";
            cm.arguments.Add(args[0]);
            s.postMessage(cm);
        }

        /*------------------------<sends the xml file name to repository in order to fetch the file from repository>---------------*/

        public void sendXmlName(string[] args, CommMessage rcv)
        {
            List<string> xml_list = new List<string>();
            xml_list.Add(args[0]);
            foreach (string s in rcv.arguments)
            {
                xml_list.Add(s);
                Console.WriteLine("\n The XML file to be parsed=" + s + " \n");
            }
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.command = "Send_XML";
            comm.author = "Chetali Mahore";
            comm.to = "http://localhost:8079/IPluggableComm";
            comm.from = "Child";
            comm.arguments.AddRange(xml_list);
            send_repo.postMessage(comm);
        }

        /*------------------------<sends the names of the dependent files after parsing the build request>---------------*/

        public void sendParsedFilesName(int process, List<string> files, string filename)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.command = "Parsing_Done";
            comm.to = "http://localhost:8079/IPluggableComm";
            comm.from = "Child";
            comm.arguments.Add(process.ToString());
            comm.arguments.Add(filename);
            comm.arguments.AddRange(files);
            send_repo.postMessage(comm);

        }

        /*-----------------<receives the reply message from repository for its request message at the initial stage>----------*/

        public void acknowledgeFirst(CommMessage rcvMsg)
        {
            List<string> xml_files = new List<string>();
            CommMessage cm = new CommMessage(CommMessage.MessageType.reply);
            cm.command = "OK";
            cm.from = "Child";
            cm.to = "http://localhost:8079/IPluggableComm";
            foreach (string s in rcvMsg.arguments)
                xml_files.Add(s);
            cm.arguments = xml_files;
            send_repo.postMessage(cm);
        }

        /*-----------------<thread which receives messages continuously from mother and repository>-----------------*/

        public void receiveContinuous(string[] args)
        {
            Thread receive_start = new Thread(() =>
            {
                savePath = savePath + "Child_" + args[0];
                temppath = savePath + "/TempStorage/";
                if (!System.IO.Directory.Exists(savePath))
                    System.IO.Directory.CreateDirectory(savePath);
                if (!System.IO.Directory.Exists(temppath))
                    System.IO.Directory.CreateDirectory(temppath);
                int count_xml = 0;
                Console.WriteLine("\n Meeting requirement 6 of project 4 where child process continues to access" +
                    " mother builder until shut down by mother \n");
                while (flag)
                {
                    CommMessage rcvMsg = receiver.getMessage();
                    rcvMsg.show();
                    if (rcvMsg.type == CommMessage.MessageType.request && rcvMsg.from == "Mother")
                        sendReadyMsgToMother(args);
                    if (rcvMsg.command == "Post_XML" && rcvMsg.from == "Mother")
                    {
                        sendXmlName(args, rcvMsg);
                    }
                    if (rcvMsg.command == "Ready_XML" && rcvMsg.from == "Repo")
                    {
                        acknowledgeFirst(rcvMsg);
                    }
                    if (rcvMsg.command == "Parse_XML" && rcvMsg.from == "Repo")
                    {
                        invokeParse(rcvMsg);
                    }
                    if (rcvMsg.command == "Files_Sent" && rcvMsg.from == "Repo")
                    {
                        createDllAndTransfer(rcvMsg, count_xml);
                    }

                    if (rcvMsg.command == "Quit" && rcvMsg.from == "Mother")
                    {
                        quit();
                        break;  
                    }
                }
            }
            );
            receive_start.Start();
        }

        /*--------------closes all the sender objects----------------*/

        public void quit()
        {
            Console.WriteLine("\n Received Quit Message from Mother Builder \n");
            CommMessage cm = new CommMessage(CommMessage.MessageType.closeSender);
            s.postMessage(cm);
            send_repo.postMessage(cm);
            send_test.postMessage(cm);
            flag = false;
            
        }

        /*---------------function for calling the createDll method and transferring build logs to repo--------*/
        public void createDllAndTransfer(CommMessage rcvMsg, int count_xml)
        {
            string dll_return = null;
            string dll_filename = null;
            Thread.Sleep(500);
            List<string> dll_list = new List<string>();
            string child_number = rcvMsg.arguments.ElementAt(0);
            string xml = rcvMsg.arguments.ElementAt(1);
            String dllpath = "../../../ChildBuild/ChildStorage/Child_" + child_number + "/";
            string temppath = "../../../ChildBuild/ChildStorage/Child_" + child_number + "/TempStorage";
            if (xml != null)
            {
                string path_of_xml = dllpath + xml;
                List<string> txt_files = new List<string>();
                Console.WriteLine("path of xml inside files sent=" + path_of_xml);
                List<string[]> hold = callParse(path_of_xml, dllpath);
                foreach (string[] arr1 in hold)
                {
                    Console.WriteLine("\n Meeting requirement 7 of project 3 where the child process attempts to build" +
                        "each library, cited in a retrieved build request, logging warnings and errors \n");
                    dll_return = createDllFile(rcvMsg, ++count_xml, arr1, temppath, dllpath, xml, txt_files);
                    if (dll_return != null)
                    {
                        dll_list.Add(dll_return);
                    }
                }
                if (dll_list.Count() > 0)
                {
                    dll_filename = createTestRequest(temppath, dll_list, child_number);
                }
                Thread.Sleep(1000);
                fileTransferToRepo(dll_filename, dll_list, child_number, txt_files);
                Thread.Sleep(1000);
            }
        }


        /*--------------transfers the build logs to repository-------------------*/

        public void fileTransferToRepo(string dll_filename, List<string> dll_list, String child_number, List<string> txt_files)
        {
            Console.WriteLine("\n Meeting requirement 8 of project 4 where the build logs are sent to the repository \n");
            ClientEnvironment.fileStorage = mainPath;
            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.from = "Child";
            cm.to = "http://localhost:8079/IPluggableComm";
            cm.command = "Sent_Build_Logs";
            cm.arguments.Add(child_number);
            cm.arguments.Add(dll_filename);
            foreach (string str in txt_files)
            {
                cm.arguments.Add(Path.GetFileName(str));
            }
            send_repo.postMessage(cm);
            Thread.Sleep(500);
            foreach (string txt_name in cm.arguments)
            {
                if (txt_name != cm.arguments.ElementAt(0) && txt_name != cm.arguments.ElementAt(1))
                {
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", txt_name));
                    bool transferSuccess = send_repo.postFile(txt_name, repopath_logs);
                    TestUtilities.checkResult(transferSuccess, "transfer");
                }

            }
            Thread.Sleep(1000);
            fileTransferToTest(dll_filename, dll_list, child_number, txt_files);
        }

        /*----------transfers the test request and the .dll files to test harness-----------------------*/

        public void fileTransferToTest(string dll_filename, List<string> dll_list, String child_number, List<string> txt_files)
        {

            Console.WriteLine("\n Meeting requirement 8 of project 4 where the case is if build succeeds," +
                " sends a test request and libraries to the Test Harness for execution \n "); 
            ClientEnvironment.fileStorage = temppath;
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.from = "Child";
            comm.to = "http://localhost:8500/IPluggableComm";
            comm.command = "Execute_dll";
            comm.arguments.Add(child_number);
            comm.arguments.Add(dll_filename);
            comm.arguments.AddRange(dll_list);
            send_test.postMessage(comm);
            Console.WriteLine("\n Destination for transferring files :" + testpath + "\n");
            foreach (string name in comm.arguments)
            {
                if (name != comm.arguments.ElementAt(0))
                {
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", name));
                    bool transferSuccess = send_test.postFile(name, testpath);
                    TestUtilities.checkResult(transferSuccess, "transfer");
                }
            }
            Thread.Sleep(500);
            cleanPath(temppath, "*.dll");
        }


        /*--------fetches the files of specific pattern--------------*/

        public static void getFiles(string pattern)
        {
            files1.Clear();
            getFilesHandler(temppath, pattern);

        }

        /*-------------finds the files in the specified directory and subdirectory and adds it to the list-----------*/

        private static void getFilesHandler(string path, string pattern)
        {

            string[] tempFiles = Directory.GetFiles(path, pattern);
            for (int i = 0; i < tempFiles.Length; ++i)
            {
                tempFiles[i] = Path.GetFullPath(tempFiles[i]);
            }
           
            files1.AddRange(tempFiles);

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                getFilesHandler(dir, pattern);
            }
        }



        /*------------------------<after receiving the xml file from repository, it parses it by calling parseForFiles function>---------------*/

        public void invokeParse(CommMessage rcvMsg)
        {
            string filename = null;
            List<string> parsed_files = new List<string>();
            int process = 0;

            foreach (string s in rcvMsg.arguments)
            {
                if (s == rcvMsg.arguments.ElementAt(0))
                    process = Int32.Parse(s);
                else
                {
                    filename = s;
                }
                parseForFiles(process, filename, parsed_files);
            }
        }


        public bool loadXml(string res)
        {
            try
            {
                doc = XDocument.Load(res);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        /*------------Parses the build request for the tests and saves each test's testdriver and testfiles in the array of tests------*/

        public List<string[]> callParse(string path_of_xml, string savePath)
        {
            loadXml(path_of_xml);
            var items = doc.Descendants("test");
            List<string[]> finallist = new List<string[]>();
            List<string> arr = new List<string>();
            string[] one;
            foreach (var item in items)
            {
                var childs = item.Descendants("testDriver").First().Value; //skip <name> element
                childs = System.IO.Path.Combine(savePath, childs);
                childs = Path.GetFullPath(childs);
                Console.WriteLine();
                arr.Add(childs);
                IEnumerable<XElement> parseElems = item.Descendants("tested");

                if (parseElems.Count() > 0)
                {
                    foreach (XElement elem in parseElems)
                    {
                        var x = System.IO.Path.Combine(savePath, elem.Value);
                        x = Path.GetFullPath(x);
                        arr.Add(x);
                    }
                }
                one = arr.ToArray();
                finallist.Add(one);
                arr.Clear();

            }
            
            return finallist;

        }


        /*---------------<it creates an object of the Parse_TestRequest class and parses the file and saves the dependent files>---------------*/

        public void parseForFiles(int process, string filename, List<string> parsed_files)
        {
            Parse_TestRequest pt = new Parse_TestRequest();
            parsed_files = pt.parse_xml(savePath, process, filename);
            sendParsedFilesName(process, parsed_files, filename);
        }

        /*-----------------<copies the file from child's main storage to child's temporary storage for creating .dll file>--------------*/

         public bool copyFiles(string s, string temppath, string dllpath)
         {
            try
            {
                string fileName = Path.GetFileName(s);
                string destSpec = Path.Combine(temppath, fileName);
                File.Copy(s, destSpec, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--", ex.Message);
                return false;
            }

        }

        /*----------------attempts to build the .dll file and saves the build results in the log file---------------------------*/

         public string createDllFile(CommMessage rcv, int count_xml, string [] temparray, string temppath, string dllpath, string xml, List<string> txt_files){
            int count = Int32.Parse(rcv.arguments.ElementAt(0));
             String savedrivername = null;
             try{
                 foreach (string s in temparray){
                     if (savedrivername == null)
                         savedrivername = s.ToString();
                     copyFiles(s, temppath+"/", dllpath);
                 }
                var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
                var cscPath = System.IO.Path.Combine(frameworkPath, "csc.exe");
                Process p = new Process();
                p.StartInfo.FileName = cscPath;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                StringBuilder builder = new StringBuilder();
                String dllname = "dll"+ DateTime.Now.ToString("HHmmssfff")+".dll";
                p.StartInfo.Arguments = "/target:library /out:" + dllname +" /warn:0 /nologo *.cs";
                string c = "../../../ChildBuild/ChildStorage/Child_" + count + "/TempStorage";
                p.StartInfo.WorkingDirectory = @c;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                string output = p.StandardOutput.ReadToEnd().ToString();
                String text_file;
                p.WaitForExit();
                if (output == ""){
                    text_file= "../../../ChildBuild/ChildStorage/SuccessfulBuild_" + xml + "_" + "Child_" + count_xml + DateTime.Now.ToString("HHmmssfff") + ".txt";
                    Console.WriteLine("Building Test {0} : Succeed...!!", count);
                    using (System.IO.StreamWriter log = new System.IO.StreamWriter(text_file)) {
                        Console.WriteLine("\n" + DateTime.Now.ToString() + ":   Build Succeeded!!!");
                        log.WriteLine(DateTime.Now.ToString() + ":   Build succeeded!!!");
                        cleanPath(temppath, "*.cs");
                        txt_files.Add(text_file);
                        return dllname;}
                }
                else{ 
                    text_file= "../../../ChildBuild/ChildStorage/BuildFail_" + xml + "_Child_" + count_xml + DateTime.Now.ToString("HHmmssfff") + ".txt";
                    using (System.IO.StreamWriter log = new System.IO.StreamWriter(text_file)) {
                        log.WriteLine(output);
                        Console.WriteLine(output);
                        output = "";
                        Console.WriteLine("Building Test {0} : Failed...", count);
                        cleanPath(temppath, "*.cs");
                        txt_files.Add(text_file);
                        return null;}
                }}
             catch (Exception e) { Console.WriteLine(e);}
            return null;
        }

        /*--------------cleans the specified pattern of files from the given path-------------------*/

        public static void cleanPath(String temppath, string pattern)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(temppath);

            foreach (FileInfo file in di.GetFiles(pattern))
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
        
        /*----------------creates the test request with the .dll files -----------*/

        public static string createTestRequest(string temppath, List<string> dll_list, string count)
        {
            string filename = "dll" + DateTime.Now.ToString("HHmmssfff") + ".xml";
            String dll_filename = temppath+"/"+ filename;
            XDocument doc1 = new XDocument();
            getFiles("*.dll");
            string[] arr = files1.ToArray();
            XElement testRequestElem = new XElement("testRequest");
            doc1.Add(testRequestElem);
            foreach (string file in arr)
            {
                string temp = Path.GetFileName(file);
                XElement testedElem = new XElement("testDriver");
                testedElem.Add(temp);
                testRequestElem.Add(testedElem);
            }

            string fileSpec = System.IO.Path.Combine(temppath+"/",  filename);
            fileSpec = System.IO.Path.GetFullPath(fileSpec);
            doc1.Save(fileSpec);
            return filename;
        }

        /*------------------------<main method>---------------*/

        public static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                ClientEnvironment.verbose = true;
                ChildBuildProgram child = new ChildBuildProgram();
                child.createChildProcess(args);
                child.receiveContinuous(args);
                return;
            }
        }
    }

    /*------------------------<parses the build request and sends back the list of dependent files>---------------*/

    public class Parse_TestRequest
    {
        private string author { get; set; } = "";
        private string dateTime { get; set; } = "";
        private string testDriver { get; set; } = "";
        private List<string> testedFiles { get; set; } = new List<string>();
        private List<string> testdriverFiles { get; set; } = new List<string>();
        private XDocument doc { get; set; } = new XDocument();

        /*------------------------<loads the XML from the specified path>---------------*/

        public bool loadXml(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n Trying to load the XML file from path :" + path + " \n");
                string s = ex.Message;
                return false;
            }
        }

        /*------------------------<parses the request, calls parseList method and returns the dependent files>----------------------------*/

        public List<string> parse_xml(string savePath, int process, string filename)
        {
            string result = savePath + "/" + filename;
            Console.WriteLine("Filename in parse_xml =" + filename);
            List<string> parsed_files = new List<string>();
            loadXml(result);
            parsed_files.AddRange(parseList("testDriver"));
            parsed_files.AddRange(parseList("tested"));
            return parsed_files;

        }

        /*---------------------<parses the build request for tested propertyName >----------------------------*/
        public string parse(string propertyName)
        {
            string testDriver1;
            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "testDriver":
                        {
                            testDriver1 = parseStr;
                        }
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }

        /*-----------------parses the build request for values of test driver and test files---------------*/

        public List<string> parseList(string propertyName)
        {
            List<string> values = new List<string>();

            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "testDriver":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testdriverFiles = values;
                        break;
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

    }

}
