
namespace NeoShell.Models
{
  class Connection
  {

    public Connection(string file, DateTime LastConnectedAt)
    {
      this.File = file;
      this.LastConnectedAt = LastConnectedAt;

    }

    public string File { get; set; }
    public DateTime LastConnectedAt { get; set; }
  }
}