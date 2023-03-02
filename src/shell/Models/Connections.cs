using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NeoShell.Models
{
  class Connections : List<Connection>
  {
    public void Upsert(string file)
    {
      var found = this.Find(c => file.Equals(c.File));
      if (found != null)
      {
        found.LastConnectedAt = System.DateTime.UtcNow;
      }
      else
      {
        this.Add(new Connection(file, System.DateTime.UtcNow));
      }
    }

    public Connection? GetMostRecent()
    {
      System.DateTime mostRecent = System.DateTime.MinValue;
      Connection? mostRecentConnection = null;
      foreach (var conn in this)
      {
        if (conn.LastConnectedAt > mostRecent)
        {
          mostRecent = conn.LastConnectedAt;
          mostRecentConnection = conn;
        }
      }
      if (mostRecentConnection != null)
      {
        mostRecentConnection.LastConnectedAt = System.DateTime.UtcNow;
      }
      return mostRecentConnection;
    }


      public static Connections FromJson(string json)
      {
        return JsonConvert.DeserializeObject<Connections>(json) ?? new Connections();
      }

      public string ToJson()
      {
        return JsonConvert.SerializeObject(this);
      }
    }
  }