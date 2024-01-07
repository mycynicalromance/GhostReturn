using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;


//이 코드에서는, 반동에 의해 Reticle Visual이 어떻게 변동하는지를 다룹니다.
//StartMoveReticle를 통해 isSpreading이 활성화 되면, 탄퍼짐이 시작됨.
public class Reticle : MMMonoBehaviour
{
    //탄퍼짐에 따른 조준점의 Spread를 활성화 할것인가?
    [SerializeField] private bool ActivateReticleSpread;

    //디버깅중. private로 바꿔주도록 하자.
    public bool isSpreading = false;

    [Tooltip("Part of Reticle")]
    [SerializeField] private Transform topReticle;
    [SerializeField] private Transform bottomReticle;
    [SerializeField] private Transform rightReticle;
    [SerializeField] private Transform leftReticle;

    [Tooltip("ReticleSpread")]
    [SerializeField] private float spreadLength;


//총 탄퍼짐 시간. 디버깅 중이니, private로 바꿔주자.
    public float totalSpreadDuration;
//탄퍼짐 시작한 뒤 경과 시간. private로 바꿔주자.
    private float elapsedTimeOfSpread=0f;
    public AnimationCurve reticleMovementCurve;
    public BaseReticlePosition baseReticlePosition;

    

    private void Start(){
        baseReticlePosition = new BaseReticlePosition(topReticle.localPosition, bottomReticle.localPosition, leftReticle.localPosition, rightReticle.localPosition);
    }

    public class BaseReticlePosition
    {
        public Vector3 TopReticleLocalPosition { get; }
        public Vector3 BottomReticleLocalPosition { get; }
        public Vector3 LeftReticleLocalPosition { get; }
        public Vector3 RightReticleLocalPosition { get; }

        public BaseReticlePosition(Vector3 _top, Vector3 _bottom, Vector3 _left, Vector3 _right)
        {
            TopReticleLocalPosition = _top;
            BottomReticleLocalPosition = _bottom;
            LeftReticleLocalPosition = _left;
            RightReticleLocalPosition = _right;
        }
    }



    private void Update()
    {
        CheckSpread();
    }

    private void CheckSpread(){
        if(isSpreading){
            MoveReticle();
        }
    }

    //이것은, Weapon클래스의 WeaponUse에 의해 실행된다.
    public void StartMoveReticle(AnimationCurve reticlemovementcurve, float duration){
        isSpreading = true;
        reticleMovementCurve = reticlemovementcurve;
        totalSpreadDuration = duration;
    }


    private void MoveReticle(){

        elapsedTimeOfSpread += Time.deltaTime;

        if (elapsedTimeOfSpread >= totalSpreadDuration){
            isSpreading = false;
            elapsedTimeOfSpread=0f;
            return;
        }

        else
        {
            // 현재 시간을 0에서 1 사이의 값으로 정규화
            float normalizedTime = elapsedTimeOfSpread / totalSpreadDuration;

            // 변화량을 애니메이션커브에서 가져와서 적용
            float normalizedMovementAmount = reticleMovementCurve.Evaluate(normalizedTime);
            float deltaReticleDistance = Mathf.Lerp(0f, spreadLength, normalizedMovementAmount);

            // 네 내조준점의 위치 업데이트
            UpdateReticlesLocation(deltaReticleDistance);
        }
    }

    private void UpdateReticlesLocation(float distance){
        topReticle.localPosition=baseReticlePosition.TopReticleLocalPosition + Vector3.up*distance;
        bottomReticle.localPosition=baseReticlePosition.BottomReticleLocalPosition + Vector3.down*distance;
        leftReticle.localPosition=baseReticlePosition.LeftReticleLocalPosition + Vector3.left * distance;
        rightReticle.localPosition=baseReticlePosition.RightReticleLocalPosition + Vector3.right * distance;
    }

}
