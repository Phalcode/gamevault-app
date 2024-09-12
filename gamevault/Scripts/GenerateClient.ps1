param (
    [string]$openApiUrl = "https://learn-csharp.alkanit.de/api/docs-json",
    [string]$outputFolder = "../Generated"
)

function Log-Message {
    param (
        [string]$message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Output "$timestamp - $message"
}

# Start script
Log-Message "Script execution started."

# Log parameters
Log-Message "OpenAPI URL: $openApiUrl"
Log-Message "Output folder: $outputFolder"

try {
	# Delete existing output folder if it exists
	if (Test-Path -Path $outputFolder) {
		Remove-Item -Path $outputFolder -Recurse -Force
		Log-Message "Existing output folder deleted: $outputFolder"
	}

    # Download OpenAPI spec JSON
    Log-Message "Downloading OpenAPI specification JSON from $openApiUrl..."
    $openApiJson = Invoke-RestMethod -Uri $openApiUrl
    Log-Message "Successfully downloaded OpenAPI specification."

    # Prepare the body for the Swagger generator API
    Log-Message "Preparing the request body for Swagger generator API..."
    $body = @{
        "spec" = $openApiJson
        "type" = "CLIENT"
        "lang" = "csharp-dotnet2"
    } | ConvertTo-Json -Depth 10
    Log-Message "Request body prepared."

    # Send request to Swagger generator API
    Log-Message "Sending request to Swagger generator API..."
    $response = Invoke-RestMethod -Uri "https://generator3.swagger.io/api/generate" -Method Post -Body $body -ContentType "application/json" -OutFile "$env:TEMP\generated.zip"
    Log-Message "Successfully downloaded the generated ZIP file."

    Log-Message "Extracting the generated ZIP file to $outputFolder..."
    Expand-Archive -Path "$env:TEMP\generated.zip" -DestinationPath $outputFolder -Force
    Log-Message "Successfully extracted the generated ZIP file to $outputFolder."

    # Cleanup
    Log-Message "Cleaning up temporary files..."
    Remove-Item -Path "$env:TEMP\generated.zip"
    Log-Message "Temporary files cleaned up."

    # Perform Regex operations on generated .cs files
    Log-Message "Starting Regex operations on generated .cs files..."

    $csFiles = Get-ChildItem -Path "$outputFolder/src" -Recurse -Filter "*.cs"

    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName
        $originalContent = $content
		
		$content = $content | Where-Object { $_ -notmatch 'Newtonsoft' }
        if ($originalContent -ne $content) {
            Log-Message "Removed lines containing 'Newtonsoft' in file: $($file.FullName)"
        }

        $content = $content | Where-Object { $_ -notmatch '///.*summary>' }
        if ($originalContent -ne $content) {
            Log-Message "Removed summary comments in file: $($file.FullName)"
        }

        $content = $content | Where-Object { $_ -notmatch '/// <value>' }
        if ($originalContent -ne $content) {
            Log-Message "Removed lines containing '/// <value>' in file: $($file.FullName)"
        }

        $content = $content | Where-Object { $_ -notmatch '\[DataMember' }
        if ($originalContent -ne $content) {
            Log-Message "Removed lines containing '[DataMember' in file: $($file.FullName)"
        }

        $content = $content | Where-Object { $_ -notmatch '\[DataContract\]' }
        if ($originalContent -ne $content) {
            Log-Message "Removed lines containing '[DataContract]' in file: $($file.FullName)"
        }

        $content = $content | Where-Object { $_ -notmatch 'presentation of the object' }
        if ($originalContent -ne $content) {
            Log-Message "Removed lines containing 'presentation of the object' in file: $($file.FullName)"
        }

        $content = $content -replace 'namespace IO.Swagger.Model', 'namespace gamevault.Models'
        if ($originalContent -ne $content) {
            Log-Message "Replaced 'namespace IO.Swagger.Model' with 'namespace gamevault.Models' in file: $($file.FullName)"
        }
		
		$content = $content -replace 'JsonProperty\(PropertyName\s*=\s*', 'JsonPropertyName('
        if ($originalContent -ne $content) {
            Log-Message "Replaced 'JsonProperty(PropertyName = '# with 'JsonPropertyName(' in file: $($file.FullName)"
        }
		
		$content = $content | Where-Object { $_ -notmatch '^\s*///\s*$' }
        if ($originalContent -ne $content) {
            Log-Message "Removed empty '///' lines in file: $($file.FullName)"
        }
		
        $content | Set-Content -Path $file.FullName
		(Get-Content $file.FullName -Raw) -replace 'public override string ToString(.|\n)*', "`n}`n}" | Set-Content -Path $file.FullName
		Log-Message "Removed ToString and ToJson Methods"
    }

    Log-Message "Regex operations completed successfully."

    # End script
    Log-Message "Script execution completed successfully."
} catch {
    Log-Message "An error occurred generating the API Client: $_"
}
Read-Host -Prompt "Press Enter to Exit"