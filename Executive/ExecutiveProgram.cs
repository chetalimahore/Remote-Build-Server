///////////////////////////////////////////////////////////////////////
// ExecutiveProgram.cs -    The function of the ExecutiveProgram is to automate the entire system. //
//                          The processes are invoked and the entire message flow is shown between them. //
//                           All the requirements are displayed onto respective processes consoles.   //  
// Author - Chetali Mahore                                              //
// Term - Fall 2017                                                   //
// Instructor- Jim Fawcett, CSE681 - Software Modeling and Analysis  //
///////////////////////////////////////////////////////////////////////
/*
* The Executive Program has a main method which instantiates a ProcessGUI class in GUI package.
* Gives a call to the functions of the ProcessGUI class and automates the processes.
* Helpful in showing the met requirements.
* 
* 
* Required Files:
* -------------------
* ExecutiveProgram.cs
* MainWindow.xaml.cs
* 
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
* 
*/



using GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GUI.MainWindow;

namespace Executive
{
    public class ExecutiveProgram
    {

        public static void Main(string[] args)
        {
            ProcessGUI processGUI = new ProcessGUI();
            processGUI.loadProcesses();
            
            List<string> test_files = new List<string>(new string[] { "Interfaces.cs", "TestedLib.cs", "TestedLibDependency.cs" });
            processGUI.generateXml("TestLib.cs", test_files);
            processGUI.appendRequest("TestRequest223245650.xml", "TestLib.cs", test_files);
            Thread.Sleep(500);
            List<string> xml_files = new List<string>(new string[] { "TestRequest1.xml", "TestRequest223302573.xml", "TestRequest223245650.xml" });
            processGUI.sendFileToRepo(xml_files);
            Thread.Sleep(50000);
            processGUI.sendQuit();
            
        }
        
    }
}
