///////////////////////////////////////////////////////////////////////
// RepositoryProgram.cs -    The Repository receives the input of build requests from the GUI //
//                           and sends it to the BuildServer. When the child process asks for the required files, //
//                            it transfers the required dependency files to that child's local storage.   //  
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
 * The repository's exe file is invoked from the GUI. The GUI sends the build request in the form of .xml file to the repository.
 * In turn, the repository sends them to mother builder. When the child process parses the test request, it asks the repository
 * to transfer the required dependent files. The Repository's address is known to each of the child process.
 * 
 *  Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.Threading;
 *   - System.IO;
 *   
 *  Package Operations:
 *  -------------------
 * This package defines two classes:
 * 
 * - Repository which implements the public methods:
 * -------------------------------------------------
 * 
 *    - createReceiver              : starts the receiver and creates senders for communicating with the child processes
 *    - rcvMessages                 : thread which receives messages continuously from the child and the GUI
 *    - createChildSenders          : it creates sender object for each of the child process
 *    - sendMsgToBuilder            : it sends message to builder along with the xml files as arguments
 *    - sendFirstMsg                : sends the initial message to child by calling msgForChannelCreation function
 *    - msgForChannelCreation       : before sending the XML file, repository sends a message for channel creation
 *    - acknowledgeChild            : sends the xml file to the child by calling sendXmlToChild function
 *    - sendXmlToChild              : sends the xml file to the child
 *    - createRequestHandler        : function to call the sendCreateRequest method which sends the created build request to GUI
 *    - sendCreateRequest           : sends the reply message of creating new test request to GUI
 *    - appendRequestHandler        : handler for appending the existing build request
 *    - sendAppendRequest           : sends the repply message of appending the existing build request to GUI
 *    - sendTestLogHandler          : handler for accepting the test logs from test harness and sending notification to GUI
 *    - sendBuildLogHandler         : handler for accepting the build logs from child builder and sending notifications to GUI
 *    - sendBuildLog                : sends build log to GUI
 *    - sendTestLog                 : sends test log to GUI
 *    - openFileForRead             : opens the file at the specified path for reading the contents of the file
 *    - getFilesFromPath            : gets the files of the specific pattern from the path and return the list of files
 *    - getFilesHelper              : fetch the files from the specified path and stores them in appropriate list
 *    - openFiles                   : function to call the sendCreateRequest method which sends the created build request to GUI
 *    - fileTransfer                : transfers the files from the repository to the child
 *    - sendQuit                    : closes the sender objects after receiving quit message from GUI
 *    
 *    
 * - RepoProgram which has a Main method for invoking the functions of the Repository class
 * ----------------------------------------------------------------------------------------
 * 
 * The Package also implements the class MPCommService with public methods:
 *  ------------------------------------------------------------
 *  
 *  - postmessage           : sender posts the message on the channel for the receiver to get the message
 *  - getmessage            : receiver gets the message from the channel
 *  - postfile              : reads the files and posts it on the filestream from where it is read 
 *  - fileTransfer         : transfers the files from source path to the destination path
 *  
 *  
 *   
 * Required Files:
 * ---------------
 * RepositoryProgram.cs
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



using MessagePassingComm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestRequest;

namespace Repository

{
    /*--------------<it creates the receiver of repository and receive messages continuously and transfer the required files to child processes>-----------*/

    public class RepositoryProgram
    {
        private Receiver receiver = new Receiver();
        private string local_address { get; set; } = "http://localhost";
        private string child_storage { get; set; } = "../../../ChildBuild/ChildStorage/";
        private string repo_storage_allFiles { get; set; } = "../../../Repository/RepoStorage/All_Files";
        private string repo_storage { get; set; } = "../../../Repository/RepoStorage";
        //private static string repo_driver { get; set; } = "../../../Repository/RepoStorage/Test_Drivers";
        //private static string repo_test { get; set; } = "../../../Repository/RepoStorage/Test_Files";

        private string repopath_logs { get; set; } = "../../../Repository/RepoStorage/Generated_Logs";
        private static List<string> upload_driver { get; set; } = new List<string>();
        private static List<string> upload_testfiles { get; set; } = new List<string>();
        private static List<string> upload_xml { get; set; } = new List<string>();
        private int port = 8079;
        private int sender_port = 8080;
        private int sender_guiport = 9000;
        private int count;
        private Sender s = null;
        private Sender[] sender = null;
        private Sender sender_gui = null;
        //private bool flag = true;

        /*----------------constructor for starting the receiver---------------------*/

       public RepositoryProgram()
        {
            try
            {
                receiver.start(local_address, port);
            }
            catch (Exception e)
            {
                string s = e.Message.ToString();
                Console.WriteLine("Process is already running on the port \n");
                Console.WriteLine("Close the process first");
               
            }
        }


        /*----------------<starts the receiver and creates senders for communicating with the chid processes>---------------*/

        public void createReceiver()
        {
            Console.Title = "Repository";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n  Repository Process \n");
            Console.Write("\n ==================== \n");
            
            s = new Sender(local_address, sender_port);
            sender = new Sender[count + 1];
            sender_gui = new Sender(local_address, sender_guiport);
            if (!System.IO.Directory.Exists(repopath_logs))
                System.IO.Directory.CreateDirectory(repopath_logs);
            
        }

        /*---------------<thread which receives messages continuously from the child and the GUI>--------------------*/

        public void rcvMessages(){
            Thread rcvMessages_thr = new Thread(() =>{
                while (true) {
                    CommMessage rcvMsg = null;
                    rcvMsg = receiver.getMessage();
                    rcvMsg.show();
                    if((rcvMsg.command== "Open_Driver" || rcvMsg.command=="Open_Test" || rcvMsg.command=="Open_XML") && rcvMsg.from=="GUI")
                        openFiles(rcvMsg.command);
                    if(rcvMsg.command== "Create_Request" && rcvMsg.from=="GUI")
                    {
                        Console.WriteLine("\n Meeting requirement 11 of project 4 where selected files from the GUI " +
               "are packaged in the test library i.e. test element specifying driver and tested files," +
               "added to a build request structure");
                        createRequestHandler(rcvMsg);
                    }
                    if(rcvMsg.command=="Append_Request" && rcvMsg.from=="GUI")
                    {
                        Console.WriteLine("\n Meeting requirement 11 of project 4 where we repeat the process of adding other test libraries to the build request structure \n");
                        Console.WriteLine("\n Meeting requirement 12 of project 4 where the client sends the request to repository for appending the existing XML build request" +
                           " and the repository stores and transmits it to the build server");
                        appendRequestHandler(rcvMsg);
                    }
                    if(rcvMsg.command=="Sending_Test_Logs" && rcvMsg.from=="TestHarness")
                        sendTestLogHandler(rcvMsg);
                    if(rcvMsg.command=="Sent_Build_Logs" && rcvMsg.from=="Child")
                        sendBuildLogHandler(rcvMsg);
                    if (rcvMsg.type != CommMessage.MessageType.connect && rcvMsg.command == "Post_XML" && rcvMsg.from=="GUI")
                    {
                        Console.WriteLine("\n Meeting requirement 13 of project 4 where the client shall be able to request the " +
                    "repository to send a build request in its storage to the build server for build processing");
                        sendMsgToBuilder(rcvMsg);
                    }
                    if (rcvMsg.command == "Send_XML" && rcvMsg.from == "Child")
                        sendFirstMsg(rcvMsg);
                    if (rcvMsg.command == "OK" && rcvMsg.from == "Child")
                        acknowledgeChild(rcvMsg);
                    if (rcvMsg.command == "Parsing_Done" && rcvMsg.from == "Child")
                        fileTransfer(rcvMsg);
                    if (rcvMsg.command == "Quit" && rcvMsg.from == "GUI")
                       sendQuit();
                }
            });
            rcvMessages_thr.Start();
            return;
        }

        /*----------function to call the sendCreateRequest method which sends the created build request to GUI-----*/

        public void createRequestHandler(CommMessage rcvMsg)
        {
            CreateRequest ex = new CreateRequest();
            string s = ex.makeRequest(rcvMsg, repo_storage_allFiles);
            sendCreateRequest(s, rcvMsg.command);
            Thread.Sleep(500);
            openFiles("Open_XML");
            Console.WriteLine("\n Meeting requirement 4 where repository allows the client browsing to find files to build, " +
                "builds an XML build request and sends the build request and the files to the Build server");
            Thread.Sleep(500);
        }

        /*------------------handler for appending the existing build request-----------------------*/

        public void appendRequestHandler(CommMessage rcvMsg)
        {
            CreateRequest ex = new CreateRequest();
            string s = ex.append_request(rcvMsg, repo_storage + "/Generated_XML");
            Console.WriteLine("\n Appended Test Request :\n");
            openFileForRead(Path.GetFullPath(Path.Combine(repo_storage_allFiles, rcvMsg.arguments.ElementAt(0))));
            sendAppendRequest(s, rcvMsg.command);
            Thread.Sleep(500);
            openFiles("Open_XML");
            Thread.Sleep(500);
        }

        /*--------------handler for accepting the test logs from test harness and sending notification to GUI------------------*/

        public void sendTestLogHandler(CommMessage rcvMsg)
        {
            Console.WriteLine(rcvMsg.arguments.ElementAt(0));
            Thread.Sleep(1000);
            openFileForRead(Path.GetFullPath(Path.Combine(repopath_logs, rcvMsg.arguments.ElementAt(0))));
            Thread.Sleep(500);
            sendTestLog(rcvMsg.arguments.ElementAt(0));
        }

        /*-------------handler for accepting the build logs from child builder and sending notifications to GUI--------- */

        public void sendBuildLogHandler(CommMessage rcvMsg)
        {
            Thread.Sleep(1000);
            openFileForRead(Path.GetFullPath(Path.Combine(repopath_logs, rcvMsg.arguments.ElementAt(2))));
            Thread.Sleep(500);
            sendBuildLog(rcvMsg.arguments.ElementAt(2));
        }

        /*---------------sends test log to GUI-----------------*/

        private void sendTestLog(string s)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.arguments.Add(s);
            comm.to = "http://localhost:9000/IPluggableComm";
            comm.from = "Repo";
            comm.command = "Display_Test_Log";
            sender_gui.postMessage(comm);
        }

        /*---------------sends build log to GUI-----------------*/

        private void sendBuildLog(string s)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.arguments.Add(s);
            comm.from = "Repo";
            comm.command = "Display_Build_Log";
            comm.to = "http://localhost:9000/IPluggableComm";
            sender_gui.postMessage(comm);
        }

        /*-----------------opens the file at the specified path for reading the contents of the file-----------------------*/

        private void openFileForRead(string path)
        {

            string[] lines = System.IO.File.ReadAllLines(@path);
            System.Console.WriteLine("Contents are = ");
            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                Console.WriteLine("\t" + line);
            }
        }

        /*-------------sends the repply message of appending the existing build request to GUI----------------*/

        private void sendAppendRequest(string s, string command)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.from = "Repo";
            comm.to = "http://localhost:9000/IPluggableComm";
            comm.command = "Append_Request";
            comm.arguments.Add(s);
            sender_gui.postMessage(comm);
        }

        /*--------------sends the reply message of creating new test request to GUI-------------------------*/

        private void sendCreateRequest(string s, string command)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.from = "Repo";
            comm.to="http://localhost:9000/IPluggableComm";
            comm.command = command;
            comm.arguments.Add(s);
            sender_gui.postMessage(comm);
        }

        /*------------gets the files of the specific pattern from the path and return the list of files------*/

        public static List<string> getFilesFromPath(string path, string pattern, List<string> upload_list)
        {
            upload_list.Clear();
            getFilesHelper(path, pattern, upload_list);
            return upload_list;

        }

        /*----------------fetches the files from the specified path and stores them in appropriate list--------*/

        private static void getFilesHelper(string path, string pattern, List<string> upload_list)
        {
            string[] tempFiles = Directory.GetFiles(path, pattern);
            for (int i = 0; i < tempFiles.Length; ++i)
            {
                tempFiles[i] = Path.GetFileName(Path.GetFullPath(tempFiles[i]));
            }
            
            upload_list.AddRange(tempFiles);

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                getFilesHelper(dir, pattern, upload_list);
            }
        }

        /*-------------based on the command, opens the specific folder to fetch the files----------------*/

        public void openFiles(string command)
        {
            try
            {
                List<string> final_files = new List<string>();
                if (command == "Open_Driver")
                {
                    final_files = getFilesFromPath(repo_storage + "/Test_Drivers", "*.cs", upload_driver);
                }
                else if (command == "Open_Test")
                {
                    final_files = getFilesFromPath(repo_storage + "/Test_Files", "*.cs", upload_testfiles);
                }
                else if (command=="Open_XML")
                {
                    final_files = getFilesFromPath(repo_storage + "/Generated_XML", "*.xml", upload_xml);
                }
                CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                comm.from = "Repo";
                comm.arguments.AddRange(final_files);
                comm.command = command;
                comm.to = "http://localhost:9000/IPluggableComm";
                sender_gui.postMessage(comm);

            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        /*---------------<sends the xml file to the child by calling sendXmlToChild function>--------------------*/

        public void acknowledgeChild(CommMessage rcvMsg)
        {
            List<string> xml_files = new List<string>();
            string process = null;
            foreach (string s in rcvMsg.arguments)
            {
                if (s == rcvMsg.arguments.ElementAt(0))
                    process = s;
                else xml_files.Add(s);
            }

            sendXmlToChild(process, xml_files);
        }

        /*---------------<sends the xml file to the child>--------------------*/


        public void sendXmlToChild(string process_no, List<string> xml_files)
        {
            string filename = null;
            int process = Int32.Parse(process_no);
            foreach (string s in xml_files)
            {
                filename = s;
            }
            string path = child_storage + "Child_" + process_no + "/";
            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.command = "Parse_XML";
            cm.from = "Repo";
            cm.to = "http://localhost:" + (sender_port + process).ToString() + "/IPluggableComm";
            cm.arguments.Add(process.ToString());
            cm.arguments.Add(filename);
            ClientEnvironment.fileStorage = repo_storage_allFiles;
            sender[process].postFile(filename, path);
            sender[process].postMessage(cm);

        }

        /*------------------------<sends the initial message to child by calling msgForChannelCreation>-------------------------------------*/

        public void sendFirstMsg(CommMessage rcvMsg)
        {
            List<string> xml_files = new List<string>();
            string process = null;
            foreach (string s in rcvMsg.arguments)
            {
                if (s == rcvMsg.arguments.ElementAt(0))
                    process = s;
                else
                {
                    xml_files.Add(s);
                }
            }

            msgForChannelCreation(process, xml_files);
        }

        /*-------------------<before sending the XML file, repository sends a message for channel creation>--------------------*/

        public void msgForChannelCreation(string process_no, List<string> xml_files)
        {
            int process = Int32.Parse(process_no);
            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.command = "Ready_XML";
            cm.to = "http://localhost:" + (sender_port + process).ToString() + "/IPluggableComm";
            cm.from = "Repo";
            cm.arguments.Add(process_no.ToString());
            cm.arguments.AddRange(xml_files);
            sender[process].postMessage(cm);
        }

        /*-------------------------<closes the sender objects after receiving quit message from GUI>--------------------*/

        public void sendQuit()
        {
            CommMessage cm_1 = new CommMessage(CommMessage.MessageType.closeSender);
            //s.postMessage(cm_1);

            CommMessage comm_msg = new CommMessage(CommMessage.MessageType.closeSender);
            for (int i = 1; i <= count; i++)
            {
                Console.WriteLine("Repository closing its child sender objects=" + i);

                comm_msg.command = "Quit";
                comm_msg.from = "Repo";
                comm_msg.to = "http://localhost:" + (sender_port + i).ToString() + "/IPluggableComm";
                sender[i].postMessage(comm_msg);
            }

        }

        /*-----------------------<transfers the files from the repository to the child>-------------------------------*/

        public void fileTransfer(CommMessage rcv)
        {
            String str = "";
            String filename;
            try{
                if (rcv.arguments!= null) {
                    int process = 0;
                    TestUtilities.vbtitle("\n File transfer \n");
                    List<string> list = new List<string>();
                    Console.WriteLine("\n The contents of the receive message arguments :\n");
                    foreach (string s in rcv.arguments) {
                        if (s == rcv.arguments.ElementAt(0))
                            process = Int32.Parse(s);
                        else if (s == rcv.arguments.ElementAt(1))
                            filename = s;
                        else{
                            Console.WriteLine(s);
                            list.Add(s);
                        }
                    }
                    string dest = child_storage + "Child_" + process;
                    ClientEnvironment.fileStorage = repo_storage_allFiles;
                    Console.WriteLine("\n Destination for transferring files :" + dest + "\n");
                    foreach (string name in list){
                        TestUtilities.putLine(string.Format("transferring file \"{0}\"", name));
                        bool transferSuccess = sender[process].postFile(name, dest);
                        TestUtilities.checkResult(transferSuccess, "transfer");
                    }
                    CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                    comm.command = "Files_Sent";
                    comm.to = "http://localhost:" + (sender_port + process).ToString() + "/IPluggableComm";
                    comm.from = "Repo";
                    comm.arguments.Add(process.ToString());
                    if(rcv.arguments.ElementAt(1)!=null){
                        Console.WriteLine("file_passed filename=" + rcv.arguments.ElementAt(1));
                        comm.arguments.Add(rcv.arguments.ElementAt(1));
                        sender[process].postMessage(comm);
                    }}}
            catch (Exception e){
                str = e.Message;
                Console.WriteLine("\n <--Trying to transfer files---> \n");
            }
        }

        /*-----------------<it creates sender object for each of the child process>-----------------*/

        public bool createChildSenders()
        {

            for (int i = 1; i <= count; i++)
            {
                sender[i] = new Sender(local_address, sender_port + i);
            }
            return true;

        }

        /*-----------------<it sends message to builder along with the xml files as arguments>--------------------*/

        public void sendMsgToBuilder(CommMessage rcv)
        {
            List<string> xml_files = new List<string>();
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.from = "Repo";
            comm.command = "Post_XML";
            comm.to = "http://localhost:8080/IPluggableComm";
            foreach (string s in rcv.arguments)
                xml_files.Add(s);
            comm.arguments.AddRange(xml_files);
            s.postMessage(comm);

        }

        /*-----------<Main class giving call to the repository class functions>-----------------------*/


        public class RepoProgram
        {
            public static void Main(string[] args)
            {
                if (args.Count() == 0)
                {
                }
                else
                {
                    RepositoryProgram rp = new RepositoryProgram();

                    Console.WriteLine("\n Repository is running on port 8079 \n");
                    rp.count = Int32.Parse(args[0]);
                    rp.createReceiver();

                    rp.createChildSenders();

                    rp.rcvMessages();

                }
            }
        }
    }
}
