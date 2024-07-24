param (
    [string]$openApiUrl = "https://baustelle.alfagun74.de/api/docs-json",
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

    # End script
    Log-Message "Script execution completed successfully."
} catch {
    Log-Message "An error occurred generating the API Client: $_"
}
