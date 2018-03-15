///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs -    It is the GUI of the project. It displays the text boxes, list boxes //
//                         and buttons. It takes the input of number of child processes to be spawned.//
//                         It allows the user to select the test driver and test files from the available list of files.
//                         It displays the generated XML list to the user and displays the build and test logs created to the user.//  
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   - System.Windows;
 *   
 * Package Operations :
 * -------------------
 * This package defines two classes:
 * 
 * - ProcessGUI which implements the public methods:
 * ----------------------------------------------------
 *  - loadProcesses       : loads the processes of GUI, repository, mother and test harness
 *  - rcvProcThread       : thread to continuously receive the messages from various modules of the system
 *  - openFile            : open the file at the specified path for reading the contents of the file
 *  - generateXml         : sends the testdriver and the tested files to Repository to build the build request
 *  - appendRequest       : sends the test driver, tested files and the selected build request to repository for appending the build request
 *  - sendFilesToRepo     : it sends the selected xml files to the repository
 *  - sendExitMsg         : it sends the quit message to the repository and the mother builder
 * 
 * - MainWindow which implements the private methods:
 * --------------------------------------------------
 * 
 *   - mother_builder_Click         : it starts the mother builder and the repository's exe
 *   - exit_builder_Click           : helps in closing the mother builder and the child processes
 *   - open_driver_Click            : loads the test driver files from the repository storage
 *   - open_test_Click              : loads the test files from the repository storage
 *   - open_xml_Click               : loads the newly created and already present xml files from the repository storage
 *   - generate_xml_Click           : it creates a test request depending on the input of files selected by the user
 *   - clear_Click                  : it clears the selection of files from the list boxes
 *   - send_files_Click             : it sends the selected XML files to the mother builder
 *   - add_test_Click               : adds a new test to the existing build request
 *   - logCodePopUp                 : listbox definition for open xml files button
 *   - initializeMessageDispatcher  : performs required actions on receiving particular command
 *   - createReqHandler             : notifies the user about successful or unsuccessful creation of build request and updates the list_xml listbox
 *   - appendReqHandler             : notifies the user about successful or unsuccessful append to the existing build request and updates the list_xml listbox
 *   - rcvProcThread                : thread to continuously receive the messages from various modules of the system
 *   - openFile                     : open the file at the specified path for reading the contents of the file
 *   - sendFilesToRepo              : it sends the selected xml files to the repository
 *   
 *  
 *  The Package also implements the class MPCommService with public methods:
 *  ------------------------------------------------------------
 *  
 *  - postmessage           : sender posts the message on the channel for the receiver to get the message
 * 
 *
 * Required Files:
 * ---------------
 * MainWindow.xaml.cs
 * CodePop.xaml.cs
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
 */


using MessagePassingComm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using TestRequest;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// Partial class MainWindow - includes a definition for buttons and text boxes on the GUI 
    /// 


    public partial class MainWindow : Window
    {
        // public XDocument doc { get; set; } = new XDocument();
        private string path { get; set; } = "..\\..\\..\\Repository\\RepoStorage\\All_Files\\";
        private string generated_path { get; set; } = "..\\..\\..\\Repository\\RepoStorage\\Generated_Logs";
        //private ProcessGUI processgui = new ProcessGUI();
        private Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        private Receiver receiver = null;
        private static string local_address { get; set; } = "http://localhost";
        private int gui_port = 9000;
        private static int port = 8079;
        private static int mainport = 8080;
        private Sender sender_repo = new Sender(local_address, port);
        private Sender sender_mother = new Sender(local_address, mainport);
        private Sender sender_test = new Sender(local_address, 8500);
        private int count = 0;

        //Sender sender = new Sender(local_address, port); 

        /*----------<constructor >------*/
        public MainWindow()
        {
            InitializeComponent();
            receiver = new Receiver();
            receiver.start(local_address, gui_port);
            initializeMessageDispatcher();
            rcvProcThread();

        }

        /*----------<textbox definition for taking the input of number of processes >------*/
        private void Enter_number_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /*----------<listbox definition for open test driver button >------*/

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /*----------<listbox definition for open test files button >------*/

        private void ListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        /*-----------<it starts the mother builder and the repository's exe>---------*/



        private void ListBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
        {

        }

        private void mother_builder_Click(object sender, RoutedEventArgs e)
        {
            if (count != 1)
            {
                string s = Enter_number.Text;
                Process proc_mother = new Process();
                ProcessStartInfo mstart = new ProcessStartInfo("..\\..\\..\\BuildServer\\bin\\debug\\BuildServer.exe");
                mstart.Arguments = s;
                Process.Start(mstart);
                Process proc_repo = new Process();
                ProcessStartInfo rstart = new ProcessStartInfo("..\\..\\..\\Repository\\bin\\debug\\Repository.exe");
                rstart.Arguments = s;
                Process.Start(rstart);
                Process proc_test = new Process();
                ProcessStartInfo tstart = new ProcessStartInfo("..\\..\\..\\TestHarness\\bin\\Debug\\TestHarness.exe");
                tstart.Arguments = s;
                Process.Start(tstart);

                mother_builder.IsEnabled = false;
                exit_builder.IsEnabled = true;
                count++;
            }
            else
            {
                MessageBox.Show("Processes are already running on the specified ports, Close them first", "Error");
            }



        }

        /*-----------<it sends the selected XML files to the mother builder>-----------*/

        private void send_files_Click(object sender, RoutedEventArgs e)
        {
            String str = null;
            try
            {
                CreateRequest ex = new CreateRequest();
                List<string> xml_files = new List<string>();
                foreach (string a in list_xml.SelectedItems)
                {
                    xml_files.Add(a);
                }
                Console.WriteLine("\n Meeting requirement 13 of project 4 where the client shall be able to request the " +
                    "repository to send a build request in its storage to the build server for build processing");
                sendFilesToRepo(xml_files);

            }
            catch (Exception ex)
            {
                str = ex.Message;
                MessageBox.Show("Couldn't send files, Try again", "Error");
            }


        }

        /*---------<loads the test driver files from the repository storage> ------*/

        private void open_driver_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n Meeting requirement 11 of project 4 where test drivers list is fetched from the repository \n");
            if (mother_builder.IsEnabled == false)
            {
                CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                comm.from = "GUI";
                comm.to = "http://localhost:8079/IPluggableComm";
                comm.command = "Open_Driver";
                sender_repo.postMessage(comm);
            }


        }

        /*--------<loads the test files from the repository storage>-------*/

        private void open_test_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n Meeting requirement 11 of project 4 where tested files list is fetched from the repository \n");
            if (mother_builder.IsEnabled == false)
            {
                CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                comm.from = "GUI";
                comm.to = "http://localhost:8079/IPluggableComm";
                comm.command = "Open_Test";
                sender_repo.postMessage(comm);
            }
        }


        /*---------<loads the newly created and already present xml files from the repository storage.>------*/

        private void open_xml_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("\n Meeting requirement 11 of project 4 where XML build requests list is fetched from the repository \n");
            if (mother_builder.IsEnabled == false)
            {
                CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                comm.from = "GUI";
                comm.to = "http://localhost:8079/IPluggableComm";
                comm.command = "Open_XML";
                sender_repo.postMessage(comm);
            }

        }

        /*-------------adds a new test to the existing build request-------------*/

        private void add_test_Click(object sender, RoutedEventArgs e)
        {
            String str1 = null;
            try
            {
                CreateRequest ex = new CreateRequest();

                List<string> test_items = new List<string>();
                string testdriver = null;
                foreach (string s in list_driver.SelectedItems)
                {
                    testdriver = s;
                }
                foreach (string a in list_test.SelectedItems)
                {
                    test_items.Add(a);
                }
                string xml_filename = list_xml.SelectedItem.ToString();
                Console.WriteLine("\n Meeting requirement 11 of project 4 where we repeat the process of adding other test libraries to the build request structure \n");
                Console.WriteLine("\n Meeting requirement 12 of project 4 where the client sends the request to repository for appending the existing XML build request" +
                   "and the repository stores and transmits it to the build server");
                CommMessage com = new CommMessage(CommMessage.MessageType.request);
                com.from = "GUI";
                com.to = "http://localhost:8079/IPluggableComm";
                com.command = "Append_Request";
                com.arguments.Add(xml_filename);
                com.arguments.Add(testdriver);
                com.arguments.AddRange(test_items);
                sender_repo.postMessage(com);
            }
            catch (Exception ex)
            {
                str1 = ex.Message;
                MessageBox.Show("Please try again", "Warning");
            }
        }



        /*---------<it creates a test request depending on the input of files selected by the user>-------*/

        private void generate_xml_Click(object sender, RoutedEventArgs e)
        {
            String str1 = null;
            try
            {
                CreateRequest ex = new CreateRequest();
                List<string> test_items = new List<string>();
                string testdriver = null;
                foreach (string s in list_driver.SelectedItems)
                {
                    testdriver = s;
                }
                foreach (string a in list_test.SelectedItems)
                {
                    test_items.Add(a);
                }
                Console.WriteLine("\n Meeting requirement 11 of project 4 where selected files from the GUI" +
                    "are packaged in the test library i.e. test element specifying driver and tested files," +
                    "added to a build request structure");
                CommMessage com = new CommMessage(CommMessage.MessageType.request);
                com.from = "GUI";
                com.to = "http://localhost:8079/IPluggableComm";
                com.command = "Create_Request";
                com.arguments.Add(testdriver);
                com.arguments.AddRange(test_items);
                sender_repo.postMessage(com);
            }
            catch (Exception ex)
            {
                str1 = ex.Message;
                MessageBox.Show("Please try again", "Warning");
            }


        }

        //<!--it clears the selection of files from the list boxes.---->

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            list_driver.UnselectAll();
            list_test.UnselectAll();
            list_xml.UnselectAll();
        }

        /*----------<listbox definition for open xml files button >------*/

        private void logCodePopUp(object sender, MouseButtonEventArgs e)
        {
            string fileName = list_log.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(generated_path, fileName);
                string contents = File.ReadAllText(path);
                CodePop popup = new CodePop();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        /*----------<helps in closing the mother and the child processes.>-------*/

        private void exit_builder_Click(object sender, RoutedEventArgs e)
        {
            sendExitMsg();
            mother_builder.IsEnabled = true;
            exit_builder.IsEnabled = false;
        }

        /*-------------performs required actions on receiving particular command---------------- */

        void initializeMessageDispatcher() {
            messageDispatcher["Open_Driver"] = (CommMessage msg) => {
                list_driver.Items.Clear();
                foreach (string file in msg.arguments) {
                    list_driver.Items.Add(file);
                }
            };
            messageDispatcher["Open_Test"] = (CommMessage msg) => {
                list_test.Items.Clear();
                foreach (string dir in msg.arguments) {
                    list_test.Items.Add(dir);
                }
            };
            messageDispatcher["Open_XML"] = (CommMessage msg) => {
                list_xml.Items.Clear();
                foreach (string file in msg.arguments) {
                    list_xml.Items.Add(file);
                }
            };
            messageDispatcher["Create_Request"] = (CommMessage msg) => {
                createReqHandler(msg);
               
            };
            messageDispatcher["Append_Request"] = (CommMessage msg) => {
                appendReqHandler(msg);
               
            };
            messageDispatcher["Display_Build_Log"] = (CommMessage msg) => {
                foreach (string file in msg.arguments) {
                    list_log.Items.Add(file);
                }
            };
            messageDispatcher["Display_Test_Log"] = (CommMessage msg) => {
                foreach (string file in msg.arguments) {
                    list_log.Items.Add(file);
                }
            };
        }

        /*------------notifies the user about successful or unsuccessful creation of build request and updates the list_xml listbox-----*/

        public void createReqHandler(CommMessage msg)
        {
            if (msg.arguments.ElementAt(0) == null)
                Console.WriteLine("Failed to build the build request");
            else
            {
                list_xml.Items.Add(msg.arguments.ElementAt(0));
                Console.WriteLine("Successfully built the build request");
            }
        }

        /*-----------notifies the user about successful or unsuccessful append to the existing build request and updates the list_xml listbox---*/

        public void appendReqHandler(CommMessage msg)
        {
            if (msg.arguments.ElementAt(0) == null)
                Console.WriteLine("\n Failed to append the build request \n");
            else
            {
                list_xml.Items.Add(msg.arguments.ElementAt(0));
                Console.WriteLine("\n Successfully appended the build request \n");
            }
        }

        /*-----------thread to continuously receive the messages from various modules of the system-----------------*/

        void rcvProcThread()
        {
            Thread thr = new Thread(() =>
              {
                  Console.Write("\n  starting client's receive thread");

                  while (true)
                  {
                      CommMessage msg = receiver.getMessage();
                      msg.show();
                      if (msg.command == null)
                          continue;
                      if (msg.command == "Display_Build_Log" && msg.from == "Repo")
                      {
                          Console.WriteLine("inside display build log=" + msg.arguments.ElementAt(0));
                          openFile(System.IO.Path.Combine(generated_path, msg.arguments.ElementAt(0)));
                      }
                      if (msg.command == "Display_Test_Log" && msg.from == "Repo")
                      {
                          openFile(System.IO.Path.Combine(generated_path, msg.arguments.ElementAt(0)));
                      }

                      if (msg.command == "Append_Request" && msg.from == "Repo")
                      {
                          Thread.Sleep(1000);
                          Console.WriteLine("\n Appended Test Request :\n");
                          openFile(System.IO.Path.Combine(path, msg.arguments.ElementAt(0)));
                      }
                      // pass the Dispatcher's action value to the main thread for execution

                      Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
                  }
              });
            thr.Start();

        }

        /*------------open the file at the specified path for reading the contents of the file -----------------*/

        private void openFile(string path)
        {

            string[] lines = System.IO.File.ReadAllLines(@path);
            System.Console.WriteLine("Contents are = ");
            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                Console.WriteLine("\t" + line);
            }
        }

        /*---------------sends the selected build request to the repository----------------*/

        public void sendFilesToRepo(List<string> xml_files)
        {
            Console.WriteLine("\n Meeting requirement 13 of project 4 where the client shall be able to request the " +
                    "repository to send a build request in its storage to the build server for build processing");
            CommMessage comm = new CommMessage(CommMessage.MessageType.request);
            comm.from = "GUI";
            comm.to = "http://localhost:8079/IPluggableComm";
            comm.command = "Post_XML";
            comm.arguments.AddRange(xml_files);
            sender_repo.postMessage(comm);

        }

        /*---------------sends the quit message to the repository, mother builder and test harness, mother closes the child builders----------*/

        public void sendExitMsg()
        {

            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.command = "Quit";
            cm.to = "http://localhost:8080/IPluggableComm";
            cm.from = "GUI";
            sender_mother.postMessage(cm);

            CommMessage cm1 = new CommMessage(CommMessage.MessageType.request);
            cm1.command = "Quit";
            cm1.to = "http://localhost:8079/IPluggableComm";
            cm1.from = "GUI";
            sender_repo.postMessage(cm1);

            CommMessage cm2 = new CommMessage(CommMessage.MessageType.request);
            cm2.command = "Quit";
            cm2.to = "http://localhost:8500/IPluggableComm";
            cm2.from = "GUI";
            sender_test.postMessage(cm2);


        }
    }


        /*-----------class contains functions to automate the entire flow for displaying requirements-------------*/

        public class ProcessGUI
        {
            private Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
            private Receiver receiver = null;
            private static string local_address { get; set; } = "http://localhost";
            private int gui_port = 9500;
            private Sender send_repo = new Sender(local_address, 8079);
            private Sender send_mother = new Sender(local_address, 8080);
            private Sender send_test = new Sender(local_address, 8500);
            private string path { get; set; } = "..\\..\\..\\Repository\\RepoStorage\\All_Files\\";
            private string generate_xml { get; set; } = "..\\..\\..\\Repository\\RepoStorage\\Generated_XML\\";
            private string generated_path { get; set; } = "..\\..\\..\\Repository\\RepoStorage\\Generated_Logs";
            private int count = 0;

            /*-----------------constructor-------------*/

            public ProcessGUI()
            {
                receiver = new Receiver();
                receiver.start(local_address, gui_port);
                rcvProcThread();
                
            }

            /*------------loads the processes of GUI, repository, mother and test harness------------*/

            public void loadProcesses()
            {
                string number_of_proc = "2";
               if(count!=1)
                {
                    Process guiprocess = new Process();
                    ProcessStartInfo gstart = new ProcessStartInfo("..\\..\\..\\GUI\\bin\\debug\\GUI.exe");
                    gstart.Arguments = number_of_proc;
                    Process.Start(gstart);
                Console.WriteLine("\n Meeting Requirement 10 of project 4 where GUI using WPF is included \n ");
                    Process proc_mother = new Process();
                    ProcessStartInfo mstart = new ProcessStartInfo("..\\..\\..\\BuildServer\\bin\\debug\\BuildServer.exe");
                    mstart.Arguments = number_of_proc;
                    Process.Start(mstart);
                    Process proc_repo = new Process();
                    ProcessStartInfo rstart = new ProcessStartInfo("..\\..\\..\\Repository\\bin\\debug\\Repository.exe");
                    rstart.Arguments = number_of_proc;
                    Process.Start(rstart);
                    Process proc_test = new Process();
                    ProcessStartInfo tstart = new ProcessStartInfo("..\\..\\..\\TestHarness\\bin\\Debug\\TestHarness.exe");
                    tstart.Arguments = number_of_proc;
                    Process.Start(tstart);
                    count++;
                }
                else { 
                    MessageBox.Show("Processes are already running on the specified ports, Close them first", "Error");

                }
            }

            /*-------------thread for continuously receiving messages from the other modules ------------*/

            public void rcvProcThread()
            {
                Thread thr = new Thread(() =>
                {
                    Console.Write("\n  starting client's receive thread");
                    
                    while (true)
                    {
                        CommMessage msg = receiver.getMessage();
                        msg.show();
                        
                        if(msg.command=="Create_Request" && msg.from=="Repo")
                        {
                        }
                        if (msg.command == "Append_Request" && msg.from == "Repo")
                        {
                            openFile(System.IO.Path.Combine(path, msg.arguments.ElementAt(0)));
                        }
                        if ((msg.command == "Display_Build_Log" || msg.command == "Display_Test_Log") && msg.from == "Repo")
                        {
                            openFile(System.IO.Path.Combine(generated_path, msg.arguments.ElementAt(0)));
                        }
                    }
                });
                thr.Start();

            }

            /*-----------opens the file at the specific path-------------*/

            private void openFile(string path)
            {
                Console.WriteLine("Reading the contents of the file {0} :", System.IO.Path.GetFileName(path));
                string[] lines = System.IO.File.ReadAllLines(@path);
                System.Console.WriteLine("Contents are = ");
                foreach (string line in lines)
                {
                    // Use a tab to indent each line of the file.
                    Console.WriteLine("\t" + line);
                }
            }

            /*---------------sends the testdriver and the tested files to Repository to build the build request-------------*/

            public void generateXml(string testdriver, List<string> test_items)
            {
                String str1 = null;
                try
                {
                    CreateRequest ex = new CreateRequest();
               // Console.WriteLine("\n Meeting requirement 11 of project 4 where selected files from the GUI" +
               //"are packaged in the test library i.e. test element specifying driver and tested files," +
               //"added to a build request structure");
                CommMessage com = new CommMessage(CommMessage.MessageType.request);
                    com.from = "GUI";
                    com.to = "http://localhost:8079/IPluggableComm";
                    com.command = "Create_Request";
                    com.arguments.Add(testdriver);
                    com.arguments.AddRange(test_items);
                    send_repo.postMessage(com);

                }
                catch (Exception ex)
                {
                    str1 = ex.Message;
                    MessageBox.Show("Please try again", "Warning");
                }
            }

            /*-----------------sends the selected build request to the repository ------------*/

            public void sendFileToRepo(List<string> xml_files)
            {
                CommMessage comm = new CommMessage(CommMessage.MessageType.request);
                comm.from = "GUI";
                comm.to = "http://localhost:8079/IPluggableComm";
                comm.command = "Post_XML";
                comm.arguments.AddRange(xml_files);
                send_repo.postMessage(comm);
            }

            /*-----------------sends the quit message to the repository, mother and test harness, mother closes child builders-----------*/

            public void sendQuit()
            {

                CommMessage cm = new CommMessage(CommMessage.MessageType.request);
                cm.command = "Quit";
                cm.to = "http://localhost:8080/IPluggableComm";
                cm.from = "GUI";
                send_mother.postMessage(cm);

                CommMessage cm1 = new CommMessage(CommMessage.MessageType.request);
                cm1.command = "Quit";
                cm1.to = "http://localhost:8079/IPluggableComm";
                cm1.from = "GUI";
                send_repo.postMessage(cm1);

                CommMessage cm2 = new CommMessage(CommMessage.MessageType.request);
                cm2.command = "Quit";
                cm2.to = "http://localhost:8500/IPluggableComm";
                cm2.from = "GUI";
                send_test.postMessage(cm2);
            }

            /*-------------sends the test driver, tested files and the selected build request to repository for appending the build request----------*/
            public void appendRequest(string xml_filename, string testdriver, List<string> test_items )
            {
                String str1 = null;
                try
                {
                    CommMessage com = new CommMessage(CommMessage.MessageType.request);
                //Console.WriteLine("\n Meeting requirement 11 of project 4 where we repeat the process of adding other test libraries to the build request structure \n");
                //Console.WriteLine("\n Meeting requirement 12 of project 4 where the client sends the request to repository for appending the existing XML build request" +
                //   "and the repository stores and transmits it to the build server");
                    com.from = "GUI";
                    com.to = "http://localhost:8079/IPluggableComm";
                    com.command = "Append_Request";
                    com.arguments.Add(xml_filename);
                    com.arguments.Add(testdriver);
                    com.arguments.AddRange(test_items);
                    send_repo.postMessage(com);
                    
                }
                catch (Exception ex)
                {
                    str1 = ex.Message;
                    MessageBox.Show("Please try again", "Warning");
                }
            }

        }
    }





   