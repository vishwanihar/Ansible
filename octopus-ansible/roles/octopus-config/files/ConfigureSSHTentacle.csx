#! "netcoreapp2.0"
#r "nuget:NetStandard.Library,2.0.0"
#r "nuget: Octopus.Client, 4.27.1"
#r "nuget: Octopus.Client.Extensibility, 3.0.1"
#r "nuget: Newtonsoft.Json, 10.0.3"
using System.Threading.Tasks;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Newtonsoft.Json;

private const char RoleDelimitor = ',';
private const int SSHDefaultPort = 22;

await AddMachineToEnvironment(ParseArgsToLinuxMachineObject(Args));

private static async Task AddMachineToEnvironment(LinuxMachineObject linuxMachine)
{
    using(var client = await OctopusAsyncClient.Create(linuxMachine.OctopusEndpoint))
    {
        var environments = await client.Repository.Environments.GetAll();

        var envId =
            environments.FirstOrDefault(e => e.Name.ToLower().Equals(linuxMachine.EnvironmentName.ToLower()))?.Id;

        if (envId == null)
        {
             Console.WriteLine($"Could not find Environment Id. Is {linuxMachine.EnvironmentName} the correct Environment name?");
        }

        var machine = await
            client.Repository.Machines.CreateOrModify(
                linuxMachine.Name,
                new SshEndpointResource{
                    AccountId = linuxMachine.AccountId,
                    DotNetCorePlatform = SshEndpointResource.CalamariDotNetCorePlatforms.Linux64,
                    Host = linuxMachine.Ip, Port = SSHDefaultPort, Fingerprint = linuxMachine.Fingerprint},
                new[] { new EnvironmentResource {Name = linuxMachine.EnvironmentName , Id = envId } },
                linuxMachine.Roles.ToArray());

        await client.Repository.Tasks.ExecuteHealthCheck(environmentId: envId, machineIds: new []{ machine.Instance.Id});

        var machineStatus = await client.Repository.Machines.GetConnectionStatus(machine.Instance);
        linuxMachine.HealthStatus = machineStatus.Status;

        Console.WriteLine(JsonConvert.SerializeObject(linuxMachine));
    }
}

private LinuxMachineObject ParseArgsToLinuxMachineObject(IList<string> args)
{
    return new LinuxMachineObject {
         OctopusEndpoint = new OctopusServerEndpoint(args[0], args[1]),
         EnvironmentName = args[2],
         Name = args[3],
         Ip = args[4],
         Fingerprint = args[5],
         AccountId  = args[6],
         Roles = args[7].Split(RoleDelimitor).ToList() };
}


private class LinuxMachineObject
{
    public OctopusServerEndpoint OctopusEndpoint { get; set;}

    public string EnvironmentName { get; set; }

    public string Name { get; set; }

    public string  Ip { get; set; }

    public string Fingerprint  { get; set; }

    public string AccountId { get; set; }

    public IList<string> Roles { get; set; }

    public string HealthStatus { get; set; }

}
