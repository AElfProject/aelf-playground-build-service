using GrainInterfaces;

namespace Grains;

public class BuildGrain : ProcessGrain<BuildRequestDto, BuildResponseDto>, IBuildGrain
{
    protected override async Task<BuildResponseDto?> DoLongRunningWorkAsync()
    {
        try
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                await Task.Delay(5000);

                return new BuildResponseDto
                {
                    Status = true,
                    Message = "Build completed successfully"
                };

                StopAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // cleanup


        }

        return new BuildResponseDto
        {
            Status = false,
            Message = "Build failed"
        };
    }
}

