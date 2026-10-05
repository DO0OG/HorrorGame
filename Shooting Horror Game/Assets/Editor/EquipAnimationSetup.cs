using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 장비 전환(총 꺼내기/집어넣기)용 Draw 상태를 ArmsAnimate_Controller에 자동 추가하는 셋업 스크립트.
/// - "ue5 Beretta Nano animation"의 "Draw 2" 클립을 재생하는 Draw 상태를 모든 레이어에 추가
/// - DrawSpeed 파라미터로 재생 방향 제어 (1 = 정재생/꺼내기, -1 = 역재생/집어넣기)
/// - 정재생이 끝나면 Idle(없으면 기본 상태)로 자동 복귀
/// 에디터 로드 시 자동 실행되며, 이미 설정돼 있으면 아무것도 하지 않음.
/// </summary>
public static class EquipAnimationSetup
{
    private const string ControllerPath = "Assets/Animations/FPS ARMS/ArmsAnimate_Controller.controller";
    private const string ClipFbxPath = "Assets/Animations/FPS ARMS/ue5 Beretta Nano animation.fbx";
    private const string ClipName = "Draw 2";
    private const string StateName = "Draw";
    private const string SpeedParam = "DrawSpeed";

    [InitializeOnLoadMethod]
    private static void AutoSetup()
    {
        EditorApplication.delayCall += () => Setup(false);
    }

    [MenuItem("Tools/Setup Equip Draw Animation")]
    private static void ManualSetup() => Setup(true);

    private static void Setup(bool verbose)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            return;

        bool dirty = false;

        if (!controller.parameters.Any(p => p.name == SpeedParam))
        {
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = SpeedParam,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            });
            dirty = true;
        }

        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ClipFbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == ClipName);
        if (clip == null)
        {
            Debug.LogWarning($"[EquipAnimationSetup] '{ClipName}' 클립을 찾지 못했습니다 : {ClipFbxPath}");
            return;
        }

        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            AnimatorStateMachine machine = layer.stateMachine;
            if (machine == null || machine.states.Any(s => s.state.name == StateName))
                continue;

            AnimatorState draw = machine.AddState(StateName);
            draw.motion = clip;
            draw.speed = 1f;
            draw.speedParameter = SpeedParam;
            draw.speedParameterActive = true;

            // 꺼내기(정재생)가 끝나면 Idle로 복귀. 역재생일 때는 exit time에 도달하지 않아 전이되지 않음
            AnimatorState idle = machine.states.FirstOrDefault(s => s.state.name == "Idle").state;
            if (idle == null)
                idle = machine.defaultState;
            if (idle != null)
            {
                AnimatorStateTransition t = draw.AddTransition(idle);
                t.hasExitTime = true;
                t.exitTime = 0.98f;
                t.hasFixedDuration = true;
                t.duration = 0.1f;
            }

            dirty = true;
        }

        if (dirty)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[EquipAnimationSetup] Draw 상태 / DrawSpeed 파라미터 설정 완료");
        }
        else if (verbose)
        {
            Debug.Log("[EquipAnimationSetup] 이미 설정되어 있습니다");
        }
    }
}
