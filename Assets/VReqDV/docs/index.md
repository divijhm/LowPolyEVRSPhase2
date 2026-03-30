# VReqDV: Model Based VR Scene Design Generation and Versioning Tool

## Steps to Setup the tool

1. Download and unzip the source code from the GitHub repository - https://github.com/VreqDV/vreqdv-tool/tree/dev (from dev branch)
2. Open Unity Hub on your local machine (version 2022.3.9f1 or later) and create a new project. Select Unity 3D Core template.
3. Installing Newtonsoft json package: In the Unity window, go to the 'Window' menu in the toolbar, and open Package Manager. On the top left corner, select the 'plus' symbol to add the package. Select 'Add from GitHub URL' and enter this URL in the popup field: com.unity.nuget.newtonsoft-json. Click on Add.
4. VReqDV source code: In the Project window on Unity screen, go to the Assets folder. Place the contents of the 'code' folder from the downloaded and unzipped source code here.
Your project structure will now have Assets/VReqDV folder.
5. At the top of the Unity Editor, locate and click on the menu 'Window' in the toolbar. Select VReqDV. VReqDV window is opened. In the window, you can see the specifications appearing at the left, labeled version 1. On selecting 'Display Mock-up', the mock-up scene for version 1 is generated and displayed on the scene editor window.
6. The project will contain a directory titled 'VReqDV/specifications', which has the model template specifications for the Bowling Alley scene. These can be modified according to the user’s requirements.
Play around with the specifications by editing them in the JSON file to understand the tool. Replace with your own specifications to create your own scenes. Make use of the versioning system to design your scene.

