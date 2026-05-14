# Test Template Generation Script
# Run this script from the repository root to test the template generation locally

Write-Host "Starting template generation..." -ForegroundColor Green

# Clean up any previous output
if (Test-Path "./template-output") {
    Write-Host "Cleaning up previous output..." -ForegroundColor Yellow
    Remove-Item -Path "./template-output" -Recurse -Force
}

# Step 1: Copy project files
Write-Host "`nStep 1: Copying project files..." -ForegroundColor Cyan

# Copy projects first
Copy-Item -Path "WinUiTemplate" -Destination "./template-output/WinUiTemplate" -Recurse -Force
Copy-Item -Path "WinUiTemplate.Core" -Destination "./template-output/WinUiTemplate.Core" -Recurse -Force
Copy-Item -Path "WinUiTemplate.Tests" -Destination "./template-output/WinUiTemplate.Tests" -Recurse -Force

# Copy solution-level files into main project's SolutionItems folder
# These will be placed at solution root via TargetFileName in the vstemplate
# Note: Use 'github' (no dot) - will be renamed to '.github' during template instantiation
New-Item -ItemType Directory -Path "./template-output/WinUiTemplate/SolutionItems/github/workflows" -Force | Out-Null

Copy-Item -Path ".github/workflows/run-unit-tests.yml" -Destination "./template-output/WinUiTemplate/SolutionItems/github/workflows/" -Force

# Copy solution file - prefer .sln, fallback to .slnx
if (Test-Path "*.sln") {
  Copy-Item -Path "*.sln" -Destination "./template-output/WinUiTemplate/SolutionItems/" -Force
} elseif (Test-Path "*.slnx") {
  Copy-Item -Path "*.slnx" -Destination "./template-output/WinUiTemplate/SolutionItems/" -Force
}

Copy-Item -Path ".gitignore" -Destination "./template-output/WinUiTemplate/SolutionItems/" -Force
Copy-Item -Path "LICENSE.txt" -Destination "./template-output/WinUiTemplate/SolutionItems/" -Force

# Remove build artifacts recursively
Get-ChildItem -Path "./template-output" -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -Path "./template-output" -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -Path "./template-output" -Directory -Recurse -Filter ".vs" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -Path "./template-output" -File -Recurse -Filter "*.user" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Project files copied successfully." -ForegroundColor Green

# Step 2: Generate vstemplate files
Write-Host "`nStep 2: Generating vstemplate files..." -ForegroundColor Cyan

function Generate-ProjectItemsXml {
    param(
        [string]$Path,
        [string]$Indent = "            ",
        [string]$RelativePath = ""
    )

    $items = Get-ChildItem -Path $Path -File
    $folders = Get-ChildItem -Path $Path -Directory

    $xml = ""

    # Process folders recursively (no Folder tags - just traverse and collect files)
    foreach ($folder in $folders) {
        $folderName = $folder.Name

        # Skip folders that shouldn't be in templates
        if ($folderName -in @("SolutionItems", "bin", "obj", ".vs")) {
            continue
        }

        $newRelativePath = if ($RelativePath) { "$RelativePath\$folderName" } else { $folderName }
        $xml += Generate-ProjectItemsXml -Path $folder.FullName -Indent $Indent -RelativePath $newRelativePath
    }

    # Process files - generate flat ProjectItem list with full paths
    foreach ($file in $items) {
        if ($file.Extension -eq ".csproj") { continue }

        $fileName = $file.Name

        # Source file is always the relative path from .vstemplate location
        $sourceFile = if ($RelativePath) { "$RelativePath\$fileName" } else { $fileName }

        # Include yml/yaml/sln/slnx for template parameter replacement
        $replaceParams = if ($file.Extension -match '\.(cs|csproj|xml|xaml|json|txt|md|manifest|appxmanifest|yml|yaml|sln|slnx)$') { "true" } else { "false" }

        # Regular files - VS will create folder structure from the path
        # TargetFileName is just the filename - the path in the content determines folder structure
        $xml += "$Indent<ProjectItem ReplaceParameters=`"$replaceParams`"`n"
        $xml += "$Indent             TargetFileName=`"$fileName`">$sourceFile</ProjectItem>`n"
    }

    return $xml
}

# Generate WinUiTemplate\MyTemplate.vstemplate
$mainCsproj = "WinUiTemplate.csproj"
$mainProjectXml = Generate-ProjectItemsXml -Path "./template-output/WinUiTemplate"

$mainTemplate = @"
<VSTemplate Version="3.0.0"
            Type="Project"
            xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">
    <TemplateData>
        <Name>WinUI3 MVVM Main Template</Name>
        <Description>The main UI project of WinUI3 MVVM Template</Description>
        <ProjectType>CSharp</ProjectType>
        <ProjectSubType></ProjectSubType>
        <SortOrder>1000</SortOrder>
        <CreateNewFolder>true</CreateNewFolder>
        <DefaultName>WinUI3 MVVM Main Template</DefaultName>
        <ProvideDefaultName>true</ProvideDefaultName>
        <LocationField>Enabled</LocationField>
        <EnableLocationBrowseButton>true</EnableLocationBrowseButton>
        <CreateInPlace>true</CreateInPlace>
        <Icon>__TemplateIcon.ico</Icon>
        <Hidden>true</Hidden>
    </TemplateData>
    <TemplateContent>
        <Project TargetFileName="`$safeprojectname`$.csproj"
                 File="$mainCsproj"
                 ReplaceParameters="true">
$mainProjectXml        </Project>
    </TemplateContent>
</VSTemplate>
"@

Set-Content -Path "./template-output/WinUiTemplate/MyTemplate.vstemplate" -Value $mainTemplate
Write-Host "  Generated WinUiTemplate\MyTemplate.vstemplate" -ForegroundColor Gray

# Generate WinUiTemplate.Core\MyTemplate.vstemplate
$coreCsproj = "WinUiTemplate.Core.csproj"
$coreProjectXml = Generate-ProjectItemsXml -Path "./template-output/WinUiTemplate.Core"

$coreTemplate = @"
<VSTemplate Version="3.0.0"
            Type="Project"
            xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">
    <TemplateData>
        <Name>WinUI3 MVVM Core Template</Name>
        <Description>The core services and business logic of WinUI3 MVVM Template</Description>
        <ProjectType>CSharp</ProjectType>
        <ProjectSubType></ProjectSubType>
        <SortOrder>1000</SortOrder>
        <CreateNewFolder>true</CreateNewFolder>
        <DefaultName>WinUI3 MVVM Core Template</DefaultName>
        <ProvideDefaultName>true</ProvideDefaultName>
        <LocationField>Enabled</LocationField>
        <EnableLocationBrowseButton>true</EnableLocationBrowseButton>
        <CreateInPlace>true</CreateInPlace>
        <Icon>__TemplateIcon.ico</Icon>
        <Hidden>true</Hidden>
    </TemplateData>
    <TemplateContent>
        <Project TargetFileName="`$safeprojectname`$.Core.csproj"
                 File="$coreCsproj"
                 ReplaceParameters="true">
$coreProjectXml        </Project>
    </TemplateContent>
</VSTemplate>
"@

Set-Content -Path "./template-output/WinUiTemplate.Core/MyTemplate.vstemplate" -Value $coreTemplate
Write-Host "  Generated WinUiTemplate.Core\MyTemplate.vstemplate" -ForegroundColor Gray

# Generate WinUiTemplate.Tests\MyTemplate.vstemplate
$testsCsproj = "WinUiTemplate.Tests.csproj"
$testsProjectXml = Generate-ProjectItemsXml -Path "./template-output/WinUiTemplate.Tests"

$testsTemplate = @"
<VSTemplate Version="3.0.0" Type="Project" xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">
  <TemplateData>
    <Name>WinUI 3 Tests</Name>
    <Description>Unit tests project with xUnit</Description>
    <ProjectType>CSharp</ProjectType>
    <Hidden>true</Hidden>
  </TemplateData>
  <TemplateContent>
      <Project TargetFileName="`$safeprojectname`$.Tests.csproj"
               File="$testsCsproj"
               ReplaceParameters="true">
$testsProjectXml        </Project>
  </TemplateContent>
</VSTemplate>
"@

Set-Content -Path "./template-output/WinUiTemplate.Tests/MyTemplate.vstemplate" -Value $testsTemplate
Write-Host "  Generated WinUiTemplate.Tests\MyTemplate.vstemplate" -ForegroundColor Gray

# Generate root MyTemplate.vstemplate
$rootTemplate = @"
<VSTemplate Version="3.0.0" Type="ProjectGroup" xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">
  <TemplateData>
    <Name>WinUI 3 MVVM Template</Name>
    <Description>A WinUI 3 project template with MVVM &amp; DI architecture</Description>
    <ProjectType>CSharp</ProjectType>
  </TemplateData>
  <TemplateContent>
      <ProjectCollection>
          <ProjectTemplateLink ProjectName="`$safeprojectname`$"
                               CopyParameters="true">
            WinUiTemplate\MyTemplate.vstemplate
          </ProjectTemplateLink>
          <ProjectTemplateLink ProjectName="`$safeprojectname`$.Core"
                               CopyParameters="true">
              WinUiTemplate.Core\MyTemplate.vstemplate
          </ProjectTemplateLink>
          <ProjectTemplateLink ProjectName="`$safeprojectname`$.Tests"
                               CopyParameters="true">
              WinUiTemplate.Tests\MyTemplate.vstemplate
          </ProjectTemplateLink>
      </ProjectCollection>
  </TemplateContent>
</VSTemplate>
"@

Set-Content -Path "./template-output/MyTemplate.vstemplate" -Value $rootTemplate
Write-Host "  Generated root MyTemplate.vstemplate" -ForegroundColor Gray

Write-Host "vstemplate files generated successfully." -ForegroundColor Green

# Step 3: Replace project name in source files
Write-Host "`nStep 3: Replacing project names in source files..." -ForegroundColor Cyan

# Replace in main WinUiTemplate project (exclude SolutionItems)
Get-ChildItem -Path "./template-output/WinUiTemplate" -Recurse -File | ForEach-Object {
  if ($_.FullName -notmatch '\\SolutionItems\\') {
    if ($_.Extension -match '\.(cs|xaml|json|manifest)$') {
      (Get-Content $_.FullName -Raw) -replace 'WinUiTemplate', '$$safeprojectname$$' | Set-Content $_.FullName -NoNewline
    }
  }
}

# Replace in Package.appxmanifest - critical for unique app identity
$manifestPath = "./template-output/WinUiTemplate/Package.appxmanifest"
if (Test-Path $manifestPath) {
  $manifest = Get-Content $manifestPath -Raw
  # Replace Identity Name with a GUID placeholder that VS will regenerate
  $manifest = $manifest -replace 'Name="[0-9a-f-]+"', 'Name="$$guid1$$"'
  # Replace PhoneProductId with another GUID placeholder
  $manifest = $manifest -replace 'PhoneProductId="[0-9a-f-]+"', 'PhoneProductId="$$guid2$$"'
  # Replace DisplayName and Description
  $manifest = $manifest -replace '<DisplayName>WinUiTemplate</DisplayName>', '<DisplayName>$$safeprojectname$$</DisplayName>'
  $manifest = $manifest -replace 'DisplayName="WinUiTemplate"', 'DisplayName="$$safeprojectname$$"'
  $manifest = $manifest -replace '<Description>WinUiTemplate</Description>', '<Description>$$safeprojectname$$</Description>'
  $manifest = $manifest -replace 'Description="WinUiTemplate"', 'Description="$$safeprojectname$$"'
  # Replace Publisher (optional - keep generic for template)
  $manifest = $manifest -replace '<PublisherDisplayName>.*?</PublisherDisplayName>', '<PublisherDisplayName>$$username$$</PublisherDisplayName>'
  $manifest = $manifest -replace 'Publisher="CN=.*?"', 'Publisher="CN=$$username$$"'
  Set-Content $manifestPath -Value $manifest -NoNewline
}

# Replace in WinUiTemplate.Core project - use $ext_safeprojectname$ for namespaces
Get-ChildItem -Path "./template-output/WinUiTemplate.Core" -Recurse -File | ForEach-Object {
  if ($_.Extension -match '\.(cs|json)$') {
    (Get-Content $_.FullName -Raw) -replace 'WinUiTemplate', '$$ext_safeprojectname$$' | Set-Content $_.FullName -NoNewline
  }
}

# Replace in WinUiTemplate.Tests project - use $ext_safeprojectname$ for namespaces and references
Get-ChildItem -Path "./template-output/WinUiTemplate.Tests" -Recurse -File | ForEach-Object {
  if ($_.Extension -match '\.(cs|json)$') {
    (Get-Content $_.FullName -Raw) -replace 'WinUiTemplate', '$$ext_safeprojectname$$' | Set-Content $_.FullName -NoNewline
  }
}

# Replace in .csproj files - these need both patterns
Get-ChildItem -Path "./template-output" -Recurse -Filter "*.csproj" | ForEach-Object {
  $content = Get-Content $_.FullName -Raw

  # For project references, use $ext_safeprojectname$ for both path and filename
  $content = $content -replace '\\WinUiTemplate\.Core\\', '\$$ext_safeprojectname$$.Core\'
  $content = $content -replace 'WinUiTemplate\.Core\.csproj', '$$ext_safeprojectname$$.Core.csproj'
  $content = $content -replace 'WinUiTemplate\.Tests\.csproj', '$$ext_safeprojectname$$.Tests.csproj'

  # For assembly names and root namespaces in the project's own file
  if ($_.Name -eq "WinUiTemplate.csproj") {
    $content = $content -replace '<RootNamespace>WinUiTemplate</RootNamespace>', '<RootNamespace>$$safeprojectname$$</RootNamespace>'
  }
  elseif ($_.Name -eq "WinUiTemplate.Core.csproj") {
    $content = $content -replace '<RootNamespace>WinUiTemplate\.Core</RootNamespace>', '<RootNamespace>$$safeprojectname$$</RootNamespace>'
  }
  elseif ($_.Name -eq "WinUiTemplate.Tests.csproj") {
    $content = $content -replace '<RootNamespace>WinUiTemplate\.Tests</RootNamespace>', '<RootNamespace>$$safeprojectname$$</RootNamespace>'

    # For Tests project, disable implicit includes and explicitly add all .cs files
    # This ensures template files are copied properly
    if ($content -notmatch '<EnableDefaultCompileItems>') {
      # Add EnableDefaultCompileItems=false to PropertyGroup
      $content = $content -replace '(<IsTestProject>true</IsTestProject>)', "`$1`n    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>"

      # Get all .cs files in the Tests directory and add them explicitly
      $testFiles = Get-ChildItem -Path "./template-output/WinUiTemplate.Tests" -Filter "*.cs" -File | ForEach-Object { $_.Name }
      $compileItems = ""
      foreach ($file in $testFiles) {
        $compileItems += "    <Compile Include=`"$file`" />`n"
      }

      # Insert ItemGroup with Compile items before </Project>
      $content = $content -replace '(</Project>)', "  <ItemGroup>`n$compileItems  </ItemGroup>`n`$1"
    }
  }

  Set-Content $_.FullName -Value $content -NoNewline
}

# Replace in solution files (now in SolutionItems folder)
Get-ChildItem -Path "./template-output/WinUiTemplate/SolutionItems" -Filter "*.sln*" -File -ErrorAction SilentlyContinue | ForEach-Object {
  (Get-Content $_.FullName -Raw) -replace 'WinUiTemplate', '$$safeprojectname$$' | Set-Content $_.FullName -NoNewline
}

Write-Host "Project names replaced successfully." -ForegroundColor Green

Write-Host "`n=== Template generation completed ===" -ForegroundColor Green
Write-Host "Output directory: ./template-output" -ForegroundColor Yellow
Write-Host "`nYou can now inspect the generated vstemplate files:" -ForegroundColor Cyan
Write-Host "  - ./template-output/MyTemplate.vstemplate (root)" -ForegroundColor Gray
Write-Host "  - ./template-output/WinUiTemplate/MyTemplate.vstemplate" -ForegroundColor Gray
Write-Host "  - ./template-output/WinUiTemplate.Core/MyTemplate.vstemplate" -ForegroundColor Gray
Write-Host "  - ./template-output/WinUiTemplate.Tests/MyTemplate.vstemplate" -ForegroundColor Gray
