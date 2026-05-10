using SpekkieClassLibrary.OBS.Types;

namespace SpekkieTwitchBot.Systems.OBS;

public interface IObsWebSocket
{
    string GetCurrentProgramScene();
    void SetCurrentProgramScene(string sceneName);
    bool GetInputMute(string inputName);
    void SetInputMute(string inputName, bool inputMuted);
    void SetInputVolume(string inputName, float inputVolume, bool inputVolumeDb = false);
    List<InputBasicInfo> GetInputList(string inputKind = "");
    int GetSceneItemId(string sceneName, string sourceName, int searchOffset);
    void SetSceneItemEnabled(string sceneName, int sceneItemId, bool sceneItemEnabled);
}
