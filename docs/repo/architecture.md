# Architecture

|||
|-------:|:----------|
| ![layers01](https://user-images.githubusercontent.com/9797472/27418819-6ed10544-56d2-11e7-95b3-110335d74db7.png) | ![layers02](https://user-images.githubusercontent.com/9797472/27418842-8564a266-56d2-11e7-92c6-456116ee0c65.png)|

## Layering Rules:

1. Assemblies cannot depend on the layer above it
2. Assemblies below the Visual Studio layer cannot depend on the Visual Studio SDK (no dependency on IVsXXX) nor any CPS-VS assembly. This layer and below should be hostable and usable outside of Visual Studio.

### Host-Agnostic Layer

|Assembly|Description|
|:-------|:----------|
|__Microsoft.VisualStudio.ProjectSystem.Managed__| Contains components that are shared between C#, F# and Visual Basic projects agnostic of host.|

### Visual Studio Layer

|Assembly|Description|
|:-------|:----------|
|__Microsoft.VisualStudio.ProjectSystem.Managed.VS__| Contains components that are shared between C#, F# and Visual Basic projects. |

### Visual Studio Designer Layer

|Assembly|Description|
|:-------|:----------|
|__Microsoft.VisualStudio.AppDesigner__| Contains the "Application Designer", which hosts property pages in a document window.|
|__Microsoft.VisualStudio.Editors__| Contains resources and settings editors, and C#, F# and Visual Basic property pages.|
