using MessagePassingComm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestRequest
{
    public class CreateRequest
    {
        private string author { get; set; } = "Chetali Mahore";
        private string dateTime { get; set; } = "";
        private string testDriver { get; set; } = "";
        private List<string> testedFiles { get; set; } = new List<string>();
        private static XDocument doc { get; set; } = null;
        private string path_files = "..\\..\\..\\Repository\\RepoStorage\\All_Files\\";
        private string generate_path = "..\\..\\..\\Repository\\RepoStorage\\Generated_XML\\";
        static string local_address = "http://localhost";
        private XElement testRequestElem = null;
        static int port = 8079;
        static int mainport = 8080;
        private Sender sender = new Sender(local_address, port);
        private Sender sender_mother = new Sender(local_address, mainport);



        /*-------------<it sends the quit message to Repository and the mother builder>-----------------*/

        /*public void send_exit_msg()
        {
            CommMessage cm_msg = new CommMessage(CommMessage.MessageType.closeReceiver);
            cm_msg.command = "Quit";
            cm_msg.from = "GUI";
            cm_msg.to = "http://localhost:8080/IPluggableComm";
            sender_mother.postMessage(cm_msg);
            Thread.Sleep(1000);

            CommMessage cm = new CommMessage(CommMessage.MessageType.closeSender);
            cm.command = "Quit";
            cm.to = "http://localhost:8080/IPluggableComm";
            cm.from = "GUI";
            sender_mother.postMessage(cm);

            CommMessage comm = new CommMessage(CommMessage.MessageType.closeReceiver);
            comm.command = "Quit";
            comm.to = "http://localhost:8079/IPluggableComm";
            comm.from = "GUI";
            sender.postMessage(comm);

            CommMessage cm1 = new CommMessage(CommMessage.MessageType.closeSender);
            cm1.command = "Quit";
            cm1.to = "http://localhost:8079/IPluggableComm";
            cm1.from = "GUI";
            sender.postMessage(cm1);
        }*/

        /*----------------<it creates a test request based on the input from the GUI>--------------------*/
        public string makeRequest(CommMessage com, string path)
        {
            string testdriver, result, filename = null;
            string filespec = null, filespec1 = null;
            List<string> testedfiles = new List<string>();
            try
            {
                doc = new XDocument();
                testdriver = com.arguments.ElementAt(0);
                foreach (string str in com.arguments)
                {
                    if(str!=com.arguments.ElementAt(0))
                    {
                        testedfiles.Add(str);
                    }
                }
                testRequestElem = new XElement("testRequest");
                doc.Add(testRequestElem);
                    result = "TestRequest" + DateTime.Now.ToString("HHmmssfff");
                    filename = result + ".xml";
                    XElement authorElem = new XElement("author");
                    authorElem.Add(author);
                    testRequestElem.Add(authorElem);
                    XElement dateTimeElem = new XElement("dateTime");
                    dateTimeElem.Add(DateTime.Now.ToString());
                    testRequestElem.Add(dateTimeElem);
                    XElement testElem = new XElement("test");
                testRequestElem.Add(testElem);
                    XElement driverElem = new XElement("testDriver");
                    driverElem.Add(testdriver);
                    testElem.Add(driverElem);
                    foreach (string file in testedfiles)
                    {
                        XElement testedElem = new XElement("tested");
                        testedElem.Add(file);
                        testElem.Add(testedElem);
                    }
                    filespec = System.IO.Path.Combine(path, filename);
                    filespec1 = System.IO.Path.Combine(generate_path, filename);
                saveXml(filespec);
                saveXml(filespec1);
                return filename;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public string append_request(CommMessage comm, string path)
        {
            List<string> testedfiles = new List<string>();
            testRequestElem = new XElement("testRequest");
            string filename = comm.arguments.ElementAt(0);
            string testdriver = comm.arguments.ElementAt(1);
            Console.WriteLine(filename);
            Console.WriteLine(testdriver);
            loadXml(path + "/" + filename);
            foreach(string s in comm.arguments)
            {
                if(s!=comm.arguments.ElementAt(0) && s!=comm.arguments.ElementAt(1))
                {
                    Console.WriteLine(s);
                    testedfiles.Add(s);
                }
            }
            XElement root = doc.Element("testRequest");
            //Console.WriteLine("root"+root);
            IEnumerable<XElement> rows = root.Descendants("test");
            XElement firstRow = rows.First();
            firstRow.AddBeforeSelf(
               new XElement("test",
               new XElement("testDriver", testdriver),
               testedfiles.Select(i => new XElement("tested", i))
               ));

            string filespec = System.IO.Path.Combine(path, filename);
            saveXml(filespec);
            File.Copy(Path.Combine(path, filename), Path.Combine(path_files, filename), true);
            Thread.Sleep(1000);
            return filename;
        }

       
        

    /*----------------<saves the XML to the specified path>--------------------*/

    public bool saveXml(string path)
        {
            try
            {
                doc.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }
        


        /*public string makeRequest(string testdriver, List<string> testedfiles, string xml_file, string path, int count)
        {
            try
            {
                if()

                string result, fileSpec=null, fileSpec1=null;
                string filename=null;
                XElement testRequestElem = new XElement("testRequest");
                doc.Add(testRequestElem);
                String[] s = null ;
                
                /*string result = null;
                foreach (char c in testdriver)
                {
                    if (c.Equals('.'))
                        break;
                    else result = result + c;
                }*/
               /* if (xml_file==null)
                {
                    result= "TestRequest" + DateTime.Now.ToString("HHmmssfff");
                    filename = result + ".xml";

                    XElement authorElem = new XElement("author");
                    authorElem.Add(author);
                    testRequestElem.Add(authorElem);

                    XElement dateTimeElem = new XElement("dateTime");
                    dateTimeElem.Add(DateTime.Now.ToString());
                    testRequestElem.Add(dateTimeElem);

                    XElement testElem = new XElement("test");
                    testRequestElem.Add(testElem);

                    XElement driverElem = new XElement("testDriver");
                    driverElem.Add(testdriver);
                    testElem.Add(driverElem);

                    foreach (string file in testedfiles)
                    {
                        XElement testedElem = new XElement("tested");
                        testedElem.Add(file);
                        testElem.Add(testedElem);
                    }
                    fileSpec = System.IO.Path.Combine(path, filename);
                    fileSpec1 = System.IO.Path.Combine(generate_path, filename);

                }
                else
                {
                    filename = xml_file;
                    Console.WriteLine(path + filename);
                    //string file = System.IO.Path.Combine(path, filename);
                    //file = System.IO.Path.GetFullPath(file);
                    s =loadXml(path+filename,count);
                    // foreach(string str in s)
                    // Console.WriteLine(str);
                    XElement root = doc.Element("testRequest");
                    IEnumerable<XElement> rows = root.Descendants("test");
                    XElement firstRow = rows.First();
                    firstRow.AddBeforeSelf(
                       new XElement("test",
                       new XElement("testDriver", testdriver),
                       testedfiles.Select(i => new XElement("tested", i))
                       ));

                    fileSpec = System.IO.Path.Combine(path, filename);
                    fileSpec1 = System.IO.Path.Combine(generate_path, filename);


                }
                
                
                saveXml(fileSpec);
                saveXml(fileSpec1);

                return filename;
            }
            catch (Exception e)
            {
                //MessageBox.Show("Couldn't create a test request", "Error");
                Console.WriteLine(e);
                return null;
            }
        }
        */



        

        /*----------------<loads the XML from the specified path>--------------------*/

        public string loadXml(string path)
        {
            //String[] s = null;
            try
            {
                Console.WriteLine("path inside loadxml="+path);
                doc = XDocument.Load(path);
                return "true";
                /*if (count == 1)
                {
                    s = Directory.GetFiles(System.IO.Path.GetFileName(path));
                    return s;
                }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in loadxml");
                Console.Write("\n--{0}--\n", ex.Message);
                return "false";
            }
            //return s;
        }

        /*----------------<sends the selected xml files to the repository>---------------------*/


        public static void Main(String [] args)
        {

        }
    }
}
