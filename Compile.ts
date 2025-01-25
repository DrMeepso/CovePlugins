
type Project = {
    projectFolder: string,
    projectName: string,
    dllName: string,
}
// list of all .net projects that need to be compiled
const Projects: Array<Project> = [
    {
        "dllName": "PersistentChalk.dll",
        "projectFolder": "PersistentChalk",
        "projectName": "PersistentChalk"
    },
    {
        "dllName": "StaticRain.dll",
        "projectFolder": "StaticRain",
        "projectName": "StaticRain"
    },
    {
        "dllName": "ChalkLogs.dll",
        "projectFolder": "ChalkLogs",
        "projectName": "ChalkLogs"
    }
]

console.log("Compiling projects...")

// for each project, compile the project and copy the dll to the output folder

// create output folder
import * as fs from 'fs'

const outputFolder = "plugins"
if (!fs.existsSync(outputFolder)) {
    fs.mkdirSync(outputFolder)
}

// fetch the cove git repository
// https://github.com/DrMeepso/WebFishingCove/tree/main

// clone the repository
import * as child_process from 'child_process'

(async () => {
    console.log("Cloning WebFishingCove repository...")
    const git_process = child_process.spawnSync("git", ["clone", "https://github.com/DrMeepso/WebFishingCove.git"])
    git_process.stdout && console.log(git_process.stdout.toString())
    console.log("Cloned WebFishingCove repository")

    await new Promise((resolve) => setTimeout(resolve, 1000))

    // compile the WebFishingCove project
    console.log("Compiling WebFishingCove project...")
    child_process.spawnSync("dotnet", ["restore", "./WebFishingCove/Cove/Cove.csproj"])

    child_process.spawnSync("dotnet", ["build", "./WebFishingCove/Cove/Cove.csproj"])

    // compile each project

    Projects.forEach(async (project) => {
        const projectPath = project.projectFolder + "/" + project.projectName + ".csproj"
        const dllPath = project.projectFolder + "/bin/Debug/net8.0/" + project.dllName
        const outputPath = outputFolder + "/" + project.dllName

        console.log(`Compiling ${project.projectName}`)

        // spawn a new process to compile the project
        child_process.spawnSync("dotnet", ["restore", projectPath])
        const compileProcess = child_process.spawnSync("dotnet", ["build", projectPath])
        // print the output of the compilation
        compileProcess.stdout && console.log(compileProcess.stdout.toString())
        fs.copyFileSync(dllPath, outputPath)

        console.log(`Compiled ${project.projectName}`)
    })

    console.log("Compilation complete")
})();