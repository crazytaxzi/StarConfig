using StarConfig;

var xml = """
<ActionMaps>
 <ActionProfiles version="1" optionsVersion="2" rebindVersion="2" profileName="HOSAS Test">
  <deviceoptions name=" VKBsim Gladiator EVO R    {0200231D-0000-0000-0000-504944564944}">
   <option input="x" deadzone="0.069300003"/>
  </deviceoptions>
  <options type="keyboard" instance="1" Product="Keyboard"/>
  <options type="joystick" instance="1" Product=" VKBsim Gladiator EVO R    {0200231D-0000-0000-0000-504944564944}"/>
  <options type="joystick" instance="2" Product=" VKBsim Gladiator EVO L    {0201231D-0000-0000-0000-504944564944}"/>
  <actionmap name="spaceship_movement">
   <action name="v_atc_loading_area_request"><rebind input="js2_button4" multiTap="2"/></action>
   <action name="v_strafe_longitudinal"><rebind input="js2_y"/></action>
   <action name="v_roll"><rebind input="js1_x"/></action>
  </actionmap>
  <actionmap name="spaceship_mining">
   <action name="v_mining_throttle"><rebind input="js2_z"/></action>
  </actionmap>
  <actionmap name="player">
   <action name="jump"><rebind input="js2_button2"/></action>
   <action name="moveforward"><rebind input="js2_ "/></action>
  </actionmap>
  <actionmap name="player_input_optical_tracking">
   <action name="foip_pushtotalk"><rebind input="kb1_lshift+np_add"/><rebind input="js2_button21"/></action>
  </actionmap>
 </ActionProfiles>
</ActionMaps>
""";

var tempRoot = Path.Combine(Path.GetTempPath(), "StarbindSmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
var file = Path.Combine(tempRoot, "layout_smoke_exported.xml");
File.WriteAllText(file, xml);

try
{
    var service = new StarbindProfileService();
    var profile = service.Load(file, new[] { new InputDevice(1, "Microsoft PC-joystick driver", 32, 6), new InputDevice(2, "Microsoft PC-joystick driver", 32, 6) });
    Require(profile.ProfileName == "HOSAS Test", "Profile name was not parsed.");
    Require(profile.Devices.Any(x => x.Instance == 1 && x.ProductName.Contains("Gladiator EVO R")), "Right VKB name was not parsed from profile options.");
    Require(profile.Devices.Any(x => x.Instance == 2 && x.ProductName.Contains("Gladiator EVO L")), "Left VKB name was not parsed from profile options.");
    Require(profile.Actions.Count(x => x.ActionName == "foip_pushtotalk") == 2, "Multiple rebinds on one action were collapsed.");
    Require(profile.Actions.Single(x => x.ActionName == "moveforward").Input == StarbindInput.Unbound, "Placeholder joystick input was not normalized to Unbound.");
    Require(profile.Actions.Single(x => x.ActionName == "v_mining_throttle").Context == "Mining", "Mining context classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "jump").Context == "On Foot", "On Foot context classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_strafe_longitudinal").Intent == "Move Forward / Backward", "Movement intent classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_atc_loading_area_request").Attributes.TryGetValue("multiTap", out var multiTap) && multiTap == "2", "Rebind attributes were not preserved.");

    var jump = profile.Actions.Single(x => x.ActionName == "jump");
    var backup = service.SaveAssignments(profile,
    [
        new BindingMutation(BindingMutationKind.RemoveInput, "On Foot", string.Empty, -1, string.Empty, "js2_button2"),
        new BindingMutation(BindingMutationKind.AddInput, "On Foot", jump.ActionMap, jump.ActionOrdinal, jump.ActionName, "js1_button9")
    ]);
    Require(File.Exists(backup), "Backup was not created.");
    var reloaded = service.Load(file, []);
    Require(reloaded.Actions.Any(x => x.ActionName == "jump" && x.Input == "js1_button9"), "New binding was not written.");
    Require(!reloaded.Actions.Any(x => x.Context == "On Foot" && x.Input == "js2_button2"), "Old state binding was not removed.");

    Console.WriteLine("Starbind profile smoke test passed.");
}
finally
{
    try { Directory.Delete(tempRoot, true); } catch { }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
