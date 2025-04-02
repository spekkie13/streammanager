using Microsoft.Extensions.Hosting;

namespace Spekkiefy;

public class SmartSpekkiefyService : BackgroundService
{
    /*
       Set up a file to manage song requests
       -> SpotifyID - Requested by - Request Count - Play Count
       When a song request is process add it to the file
       Set up a service to periodically take the song with the lowest plays or the highest request count 
       from the file and add it to the queue
       -> this should increase the play count so it doesn't get requested too frequently
     */
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            
        }
        throw new NotImplementedException();
    }
}