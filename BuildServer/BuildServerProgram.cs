///////////////////////////////////////////////////////////////////////
// BuildServerProgram.cs -    Swapns the child processes. It assigns the build request to child processes.//
//                            The child processes parse the test request and load the       //
//                             dependency files from the repository.    //
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
 * The BuildServer receives the build requests and the count of child processes to be spawned from the GUI. 
 * The BuildServer maintains a build request queue and a readyqueue. The build request queue stores the build requests 
 * and the readyqueue contains the process id of the child processes which are available for processing the test request.
 * We have used a 'push' model in which the mother builder sends the received build requests to the available child processes
 * as and when needed.
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
 *  SpawnProc which implements the public methods:
 *  ---------------------------------------------
 *   - createProcess          : it creates the child processes depending on the input received from GUI
 *   - spawnCall              : gives a call to createProcess method and enques the processes in the ready queue
 *   - receiveMessages        : thread which receives messages from repository and child continuously
 *   - insertIntoBlockQueue   : inserting xml file received from repository into the build request queue
 *   - checkBuildQueue        : checks the build queue continuously for available child processes
 *   - sendToChild            : assigns the XML file to the child process which is available
 *   - sendQuit              : mother builder sends the quit message to child processes and closes its own sender objects
 *   
 * The Package also implements the class MPCommService with public methods:
 *  ------------------------------------------------------------
 *  
 *  - postmessage           : sender posts the message on the channel for the receiver to get the message
 *  - getmessage            : receiver gets the message from the channel
 *   
 * Required Files:
 * ---------------
 * BuildServerProgram.cs
 * MPCommService.cs
 * IMPCommService.cs
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
 * 
 * 
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using SWTools;
using System.Threading;

namespace BuildServer
{
    /*-------<It spawns the child processes, sends build request to child and maintains the state of child in readyqueue>----------*/

    public class SpawnProc
    {

        private SWTools.BlockingQueue<int> readyqueue { get; set; } = new SWTools.BlockingQueue<int>();
        private SWTools.BlockingQueue<string> buildqueue { get; set; } = new SWTools.BlockingQueue<string>();
        private string baseAddress { get; set; } = "http://localhost";
        private int mainport { get; set; } = 8080;
        private int count;
        private Sender[] sender = null;
        private Receiver receiver = new Receiver();

        /*----------constructor for starting the receiver------------*/

        public SpawnProc()
        {
            try
            {
                receiver.start(baseAddress, mainport);
            }
            catch (Exception e)
            {
                string s = e.Message.ToString();
                Console.WriteLine("Process is already running on the port \n");
                Console.WriteLine("Close the process first");
            }
        }


        /*---------------<It spawns the child processes depending on the input received from GUI>-------------------*/

        public static bool createProcess(int i, int j)
        {
            Console.WriteLine("\n Meeting requirement 5 of project 4 where a process pool component of " +
                "specified number of child processes is created \n");
            Process proc = new Process();
            ProcessStartInfo pstart = new ProcessStartInfo("..\\..\\..\\ChildBuild\\bin\\debug\\ChildBuild.exe");
            string fileName = "..\\..\\..\\ChildBuild\\bin\\debug\\ChildBuild.exe";
            string absFileSpec = Path.GetFullPath(fileName);

            Console.Write("\n  attempting to start {0}", absFileSpec);
            string commandline = i.ToString();
            string port = j.ToString();
            try
            {
                pstart.Arguments = commandline + " " + port;
                Process.Start(pstart);

            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        /*-----------<gives a call to createprocess method and enques the processes in the ready queue>-----------*/

        public int spawnCall(string number)
        {
            Console.Title = "Mother Builder";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n  Demo Parent Process");
            Console.Write("\n =====================");
            count = Int32.Parse(number);
            sender = new Sender[count + 1];

            int process_id = 0;

            

            for (int i = 1; i <= count; i = i + 1)
            {
                process_id = process_id + 1;
                if (SpawnProc.createProcess(process_id, mainport + i))
                {
                    readyqueue.enQ(process_id);
                    sender[i] = new Sender(baseAddress, mainport + i);
                    Console.Write(" - succeeded");
                }
                else
                {
                    Console.Write(" - failed");
                }
            }
            return count;
        }

        /*--------------------------<thread which receives messages from repository and child continuously>------------------------*/

        public void receiveMessages()
        {
            Thread threadreceive = new Thread(() =>
            {

                while (true)
                {
                    CommMessage m = receiver.getMessage();
                    m.show();
                    if (m.from == "Repo" && m.command == "Post_XML")
                    {
                        insertIntoBlockQueue(m);
                    }

                    if (m.from == "Child" && m.command == "Ready")
                    {
                        foreach (string s in m.arguments)
                        {
                            readyqueue.enQ(Int32.Parse(s));
                        }
                    }
                    if (m.command == "Quit" && m.from == "GUI")
                    {
                        sendQuit();
                        //flag = false;
                        //break;
                    }
                }
            });
            threadreceive.Start();
        }

        /*--------------------<mother builder sends the quit message to child processes and closes its own sender objects>---------------*/

        public void sendQuit()
        {

            for (int i = 1; i <= count; i++)
            {
                CommMessage cm_msg = new CommMessage(CommMessage.MessageType.closeReceiver);
                cm_msg.to = "http://localhost:" + (mainport + i).ToString() + "/IPluggableComm";
                cm_msg.from = "Mother";
                cm_msg.command = "Quit";
                cm_msg.arguments.Add(i.ToString());
                sender[i].postMessage(cm_msg);
                Thread.Sleep(1000);
            }
            for (int i = 1; i <= count; i++)
            {
                CommMessage cm = new CommMessage(CommMessage.MessageType.closeSender);
                cm.command = "Quit";
                cm.from = "Mother";
                cm.to = "http://localhost:" + (mainport + i).ToString() + "/IPluggableComm";
                cm.arguments.Add(i.ToString());
                sender[i].postMessage(cm);
                //Thread.Sleep(1000);
            }

        }

        /*-------------------<assigns the XML file to the child process which is available>-----------*/

        public CommMessage sendToChild(int process_no, string s)
        {
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.to = "http://localhost:" + (mainport + process_no).ToString() + "/IPluggableComm";
            comm.arguments.Add(s);
            comm.command = "Post_XML";
            comm.from = "Mother";
            return comm;
        }

        /*------------------<checks the build queue continuously for available child processes>-------------------*/

        public void checkBuildQueue()
        {
            Thread check_thread = new Thread(() =>
            {
                while (true)
                {
                    if (buildqueue.size() > 0)
                    {
                        if (readyqueue.size() > 0)
                        {
                            string a = buildqueue.deQ();
                            int process_no = readyqueue.deQ();
                            Console.WriteLine("\n Process number available =" + process_no + "\n");
                            Console.WriteLine("\n Assigning " + a + " from mother to process = " + process_no + "\n");
                            Console.WriteLine("\n Meeting requirement 3 of project 4 where communication service supports accessing build requests by Pool Processes from mother builder ");
                            CommMessage comm = sendToChild(process_no, a);
                            sender[process_no].postMessage(comm);
                        }
                    }
                }

            });
            check_thread.Start();
        }

        /*----------------<inserting xml file received from repository into the build request queue>---------------*/

        public void insertIntoBlockQueue(CommMessage m)
        {
            foreach (string a in m.arguments)
            {
                Console.WriteLine("\n Inserting" + a + " into build queue \n");
                buildqueue.enQ(a);
            }
        }
    }

    /*---------------<main class giving call to all the above functions>-----------------*/

    public class BuildServerProgram
    {
        public static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                SpawnProc sproc = new SpawnProc();
                Console.WriteLine("\n Mother Builder is running on 8080 port \n");
               
                sproc.receiveMessages();                 //continuously check for ready processes and input them into a readyqueue
                sproc.spawnCall(args[0]);              //spawn the processes
                sproc.checkBuildQueue();                //continously check build queue for assigning processes with the build req
            }
            
        }
    }
}



