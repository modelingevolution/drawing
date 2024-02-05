New-Cake -Name "ModelingEvolution.Drawing" -Root "../Sources/ModelingEvolution.Drawing"

Add-CakeStep -Name "Build All" -Action {  Build-Dotnet -All  }
Add-CakeStep -Name "Publish to nuget.org" -Action { 
	Add-ApiToken "https://nuget.org" "oy2gob3hlzwww46y5ggq5gp6mwyx5cwebpodezn7zbhz4i"
	Publish-Nuget -SourceUrl "https://nuget.org" 
}
