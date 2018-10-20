# Remote-Build-Server

Author: Chetali Ashok Mahore
SUID - 750500177
Project 4
CSE 681- Software Modeling and Analysis

Instructions:
1. Go to the specific path where Project4.sln is saved.
2. Write the command of compile.bat
3. The files will be compiled. 
4. Now run the command of run.bat
5. Executive opens and all the automated message passing happens.
6. For creating a successful build request, select TestLib.cs as testdriver and TestedLib.cs, Interfaces.cs and TestedLibDependency.cs as tested files.
7. For creating a failure build request, select TestedLibFail.cs as testdriver and TestedLib.cs, TestedLibDependency.cs and TestedLibDependencyFail.cs as tested files.
8. Once the executive finishes execution, the child processes will shut down within 50 seconds. I have introduced the delay so that the child and test harness finish their execution and return back the log files to repository.
9. Close all the processes once the executive finishes execution since the processes cannot be started on the same port again.
10. After closing all the processes, again the executive can be run, otherwise use GUI by going into visual studio and starting GUI as start up project.
11. GUI allows you to create, append the build request.
12. CLear button clears the selected files.
13. Send_XML button helps the user to select one file and send it. For sending multiple files, select the file each time and press send xml button.
14. On clicking exit builder process, child processes will close while mother builder, repository and test harness remain open.
15. To spawn the processes again, close all the processes and start them again.

