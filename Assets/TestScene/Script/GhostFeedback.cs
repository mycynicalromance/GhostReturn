using MoreMountains.CorgiEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//????????, ???? ??? ?? ???????? ???????, ????? ????? ???? ?? ???????? ???? ???? ????.
public class GhostFeedback : MonoBehaviour
{
    [Header("Ghost Information")]
    [SerializeField] private float ghostDelay;
    [SerializeField] private GameObject ghostOriginal;
    [SerializeField] private GameObject modelVisual;
    [SerializeField] private float durationTillDestroy;
    private float ghostDelaySeconds;

    private Character _character;
    private Material originalMaterial;
    private SpriteRenderer ghostOriginalSpriteRenderer;
    private DamageOnTouch damageOnTouch;
    SpriteRenderer characterSpriteRenderer;
    // Start is called before the first frame update

    void Start()
    {
        //Character 코드의 StateMachine 값을 받아오기 위해 인스턴스를 만들고 참조.
        _character=GetComponentInParent<Character>();

        ghostDelaySeconds = ghostDelay;
        damageOnTouch = GetComponent<DamageOnTouch>();
    }

  

    // Update is called once per frame
    void Update() {
        if (_character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing) {
            if (ghostDelaySeconds > 0) {
                ghostDelaySeconds -= Time.deltaTime;
            }
            else {
                //?????? ??, GhostDelaySeconds?? ??? ???? ???.
                ghostDelaySeconds = ghostDelay;
                //Ghost ??? ????
                GameObject currentGhost = Instantiate(ghostOriginal, transform.position, transform.rotation);
                //currentGhost ????? Sprite?? ???? Sprite?? ????
                SpriteRenderer ghostSpriteRenderer = currentGhost.GetComponent<SpriteRenderer>();
                SpriteRenderer modelSpriteRenderer = modelVisual.GetComponent<SpriteRenderer>();
                ghostSpriteRenderer.sprite = modelSpriteRenderer.sprite;
                //ĳ????? ???? ?????, ghost?? ???? ?????? ????? ???.
                // 이부분은 Character의 flip을 나타내는 것을 보고 고쳐야 할듯.
                //ghostSpriteRenderer.flipX = GetComponent<SpriteRenderer>().flipX;
                //??? ??? ???.
                currentGhost.transform.localScale = modelVisual.transform.localScale;
                //durationTillDestroy ?ð??? ?????? ???????? ??.
                Destroy(currentGhost, durationTillDestroy);
            }
        }
    }
}
