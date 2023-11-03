<a name="readme-top"></a>
<div align="center">
    <a href="https://github.com/liris-xr/Unity-Runtime-GUID">
        <picture>
            <source media="(prefers-color-scheme: dark)" srcset="/Documentation~/Images/logo_dark.png">
            <source media="(prefers-color-scheme: light)" srcset="/Documentation~/Images/logo_light.png">
            <img alt="logo" src="/Documentation~/Images/logo_light.png">
        </picture>
    </a>
    <p align="center">
        <a href="https://github.com/liris-xr/Unity-Runtime-GUID/issues">Report Bug</a>
        ·
        <a href="https://github.com/liris-xr/Unity-Runtime-GUID/issues">Request Feature</a>
    </p>
</div>

<details>
    <summary>Table of Contents</summary>
    <ol>
        <li><a href="#about-the-project">About The Project</a></li>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#usage">Usage</a></li>
        <li><a href="#contributing">Contributing</a></li>
        <li><a href="#license">License</a></li>
        <li><a href="#contact">Contact</a></li>
    </ol>
</details>

## About

The UnityEngine uses a GUID system to uniquely identify objects across executions. However, this implementation is not accessible at runtime.
This package implements a custom GUID system to uniquely identify objects across executions without the need for any custom component attached to the objects.
The registries need to be updated in the editor before the application is built. New objects created at runtime will get a randomly generated GUID assigned to them.

## Installation

In the Unity Package Manager, click the `+` button and `Add package from git URL...` then enter the URL of this repository: `https://github.com/liris-xr/Unity-Runtime-GUID.git`.

## Usage

From a `UnityEditor` script, update the registries using the `GuidRegistryUpdater`. For instance on pre-build or before entering play mode in the editor.

<details>
<summary>Click to see an example</summary>

```csharp
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityRuntimeGuid.Editor;

[InitializeOnLoad]
public class MyRegistryUpdater : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MaxValue;

    static MyRegistryUpdater()
    {
        // Update the registries when entering play mode in the Editor
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            GuidRegistryUpdater.UpdateAssetsGuidRegistry(GuidRegistryUpdater.GetAllScenePaths(true));
            GuidRegistryUpdater.UpdateScenesGuidRegistry(GuidRegistryUpdater.GetAllScenePaths(true));
        };
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        // Update the registries when building the application
        GuidRegistryUpdater.UpdateAssetsGuidRegistry(GuidRegistryUpdater.GetAllScenePaths(false));
        GuidRegistryUpdater.UpdateScenesGuidRegistry(GuidRegistryUpdater.GetAllScenePaths(false));
    }
}
```

</details>

## Roadmap

See the [open issues](https://github.com/liris-xr/Unity-Runtime-GUID/issues) for a full list of proposed features (and
known issues).

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any
contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also
simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the GNU General Public License v3.0. See `LICENSE.md` for more information.

## Contact

Charles JAVERLIAT - charles.javerliat@gmail.com