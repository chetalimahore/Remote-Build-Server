///////////////////////////////////////////////////////////////////////
// TestHarnessProgram.cs -    The child processes sends the dll files to the test harness for execution.//
//                           The Test Harness executes them and sends back the test logs to repository and prints notifications on the GUI.   //
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
 * The child processes attempts to build the build request. After successful building, the child process
 * creates a test request with the .dll files in it. The test request along with the .dll files are transferred 
 * to the test harness for execution. The test harness executes each of the .dll file and creates log for each
 * of the file. It then sends it to repository for storage and displays the result on the GUI console.
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
 *  TestHarnessProgram which implements the public methods:
 *  -------------------------------------------------------
 *  - parseXmlForDll    :   Instantiating the DLLLoadExec class for executing .dll file and transferring the test logs to repository
 *  - loadXml           :   Loads the test request xml transferred to Test Harness Storage
 *  - parseList         :   Parses the test request xml and loads the .dll files
 *  
 *  
 *  DllLoaderExec which implements the public methods:
 *  -------------------------------------------------------
 *  - LoadFromComponentLibFolder :  An event handler for binding errors when loading libraries.
 *  - loadAndExerciseTesters     :  Loads assemblies from testersLocation and run their tests
 *  - runSimulatedTest           :  Run tester t from assembly asm
 *  - GuessTestersParentDir      :  Extract name of current directory without its parents
 *   
 * The Package also implements the class MPCommService with public methods:
 *  ------------------------------------------------------------
 *  
 *  - postmessage           : sender posts the message on the channel for the receiver to get the message
 *  - getmessage            : receiver gets the message from the channel
 *   
 * Required Files:
 * ---------------
 * TestHarnessProgram.cs
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

using MessagePassingComm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using SWTools;
using System.Xml.Linq;

namespace TestHarness
{
    public class TestHarnessProgram
    {
        /*--------------<it creates the receiver of test harness and receive messages continuously and transfer the test logs to repository>-----------*/

        private Receiver receiver = new Receiver();
        private static string local_address { get; set; } = "http://localhost";
        private int port { get; set; } = 8500;
        private string testpath { get; set; } = "../../../TestHarness/TestHarnessStorage/";
        private string repo_path { get; set; } = "../../../Repository/RepoStorage/Generated_Logs";
        private XDocument doc { get; set; } = null;
        private Sender send_to_repo { get; set; } = null;

        /*------------constructor for starting the receiver---------------*/

        public TestHarnessProgram()
        {
            try
            {
                receiver.start(local_address, port);
                Console.Title = "TestHarness";
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("\n  Test Harness Process \n");
                Console.Write("\n ==================== \n");

                send_to_repo = new Sender(local_address, 8079);
                
            }
            catch (Exception e)
            {
                string s = e.Message.ToString();
                Console.WriteLine("Process is already running on the port \n");
                Console.WriteLine("Close the process first");
            }
        }
        
        /*-----------Thread which continuously receive messgaes from the Child-------------*/
        
        public void receiveContinuous(string[] args)
        {
            Thread thr = new Thread(() =>
              {
                  if (!System.IO.Directory.Exists(testpath))
                      System.IO.Directory.CreateDirectory(testpath);
                  while (true)
                  {
                      CommMessage rcvMsg = receiver.getMessage();
                      rcvMsg.show();
                      if (rcvMsg.command == "Execute_dll" && rcvMsg.from == "Child")
                      {
                          Thread.Sleep(500);
                          parseXmlForDll(rcvMsg);

                          Thread.Sleep(500);
                      }
                      if(rcvMsg.command=="Quit" && rcvMsg.from=="GUI")
                      {
                          Console.WriteLine("Child builders are closing");
                      }

                  }
              });
            thr.Start();
        }


        /*------------------Calling the .dll execute function and transferring the test logs to repository------------------------------*/

        public void parseXmlForDll(CommMessage rcv)
        {
            List<string> parsed_files = new List<string>();
            loadXml(testpath + rcv.arguments.ElementAt(1));
            parsed_files.AddRange(parseList("testDriver"));
            DllLoaderExec dllexe = new DllLoaderExec();
            List<string> txt_files = new List<string>();
            foreach (string s in parsed_files)
            {
                Console.WriteLine("parsed file in parseXMl for dll function=" + s);
                testpath = Path.GetFullPath(testpath);
                Console.WriteLine("\n Meeting requirement 9 of project 4 where the test harness attempts to load each test library it receives" +
                    "and executes it. \n");
                txt_files.Add(Path.GetFileName(dllexe.loadAndExerciseTesters(testpath, s)));
            }
            Thread.Sleep(500);
            Console.WriteLine("\n Meeting requirement 9 of project 4 where results of testing are submitted to the repository \n");
            ClientEnvironment.fileStorage = testpath;
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.to= "http://localhost:8079/IPluggableComm";
            comm.from = "TestHarness";
            comm.command = "Sending_Test_Logs";
            comm.arguments.AddRange(txt_files);
            send_to_repo.postMessage(comm);
            Thread.Sleep(500);
            foreach (string file in comm.arguments)
            {
                {
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                    bool transferSuccess = send_to_repo.postFile(file, repo_path);
                    TestUtilities.checkResult(transferSuccess, "transfer");
                }
            }
        }

        /*--------------Loads the test request xml transferred to the test harness storage ------------*/

        public bool loadXml(string res)
        {
            try
            {
                doc = XDocument.Load(res);
                Console.WriteLine("inside load xml" + res);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        /*------------------Parses the test request xml and loads the .dll files------------------------------*/


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
                        //testdriverFiles = values;
                        break;
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        //testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        /*----------main function--------------*/

        public static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                
                TestHarnessProgram tprog = new TestHarnessProgram();
                Console.WriteLine("\n Test Harness is running on 8500 port \n");
                //tprog.startHarness();
                tprog.receiveContinuous(args);
            }

        }

    }
    
    /*----------------loads the .dll file into assembly, executes it and saves the results in the log files------------*/

    class DllLoaderExec
    {
        public static string finalLocation { get; set; } = "../../../TestHarness/TestHarnessStorage/";

        /*----< library binding error event handler >------------------*/
        /*
         *  This function is an event handler for binding errors when
         *  loading libraries.  These occur when a loaded library has
         *  dependent libraries that are not located in the directory
         *  where the Executable is running.
         */
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            Console.Write("\n  called binding error event handler");
            string folderPath = finalLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
        //----< load assemblies from testersLocation and run their tests >-----

        public string loadAndExerciseTesters(string finalLocation, string dll_name)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
            string txt_name = "../../../TestHarness/TestHarnessStorage/" + dll_name + "_" + DateTime.Now.ToString("HHmmssfff") + ".txt";
            try
            {
                DllLoaderExec loader = new DllLoaderExec();
               
                string[] files = Directory.GetFiles(finalLocation, dll_name);
                foreach (string file in files)
                {
                    Assembly asm = Assembly.LoadFile(file);

                    string fileName = Path.GetFileName(file);
                    Console.Write("\n  loaded {0}", fileName);

                    // exercise each tester found in assembly
                    
                    Type[] types = asm.GetTypes();
                    using (System.IO.StreamWriter log = File.AppendText(txt_name))
                    {
                        log.WriteLine("Test Harness Log :\n Filename {0} \n ", fileName);
                        foreach (Type t in types)
                        {
                            // if type supports ITest interface then run test

                            if (t.GetInterface("DllLoaderDemo.ITest", true) != null)
                                if (!loader.runSimulatedTest(t, asm))

                                {
                                    Console.Write("\n  test {0} failed to run", t.ToString());
                                        log.WriteLine(DateTime.Now.ToString() + ":  \n  test {0} failed to run", t.ToString(), true);
                                }
                                else
                                {
                                    log.WriteLine(DateTime.Now.ToString() + ":  \n  test {0} succeeded to run", t.ToString(), true);
                                }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            Console.WriteLine("Simulated Testing completed");
            return txt_name;
        }
        // 
        //----< run tester t from assembly asm >-------------------------------

        bool runSimulatedTest(Type t, Assembly asm)
        {
            try
            {
                Console.Write(
                  "\n  attempting to create instance of {0}", t.ToString()
                  );
                object obj = asm.CreateInstance(t.ToString());

                // announce test

                MethodInfo method = t.GetMethod("say");
                if (method != null)
                    method.Invoke(obj, new object[0]);

                // run test

                bool status = false;
                method = t.GetMethod("test");
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);

                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                        return "passed";
                    return "failed";
                };
                Console.Write("\n  test {0}", act(status));
            }
            catch (Exception ex)
            {
                Console.Write("\n  test failed with message \"{0}\"", ex.Message);
                return false;
            }

            return true;
        }
        // 
        //----< extract name of current directory without its parents ---------

        string GuessTestersParentDir()
        {
            string dir = Directory.GetCurrentDirectory();
            int pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
            string name = dir.Remove(0, pos + 1).ToLower();
            if (name == "debug")
                return "../..";
            else
                return ".";
        }


    }
}
