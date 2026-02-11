using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Speed")]
    public float idleSpeed = 1.0f;      // フラフラ中の速度
    public float chaseSpeed = 2.5f;     // 追跡中の速度（プレイヤー5なら半分の2.5）

    [Header("Sight")]
    public float viewRadius = 4f;       // 索敵距離
    [Range(0f, 360f)]
    public float viewAngle = 120f;      // 視野角（度）

    [Header("Idle Wander")]
    public float wanderRadius = 4f;         // スポーン位置からの最大ふらつき距離
    public float directionChangeInterval = 2f; // 何秒ごとに方向変更するか

    [Header("Chase")]
    public float loseSightTime = 1.5f;  // 視界から外れてどのくらいで追跡中止するか

    Transform player;
    
    Vector3 spawnPosition;
    Vector2 currentDir = Vector2.zero;
    float dirTimer = 0f;

    bool isChasing = false;
    float loseTimer = 0f;

    void Start()
    {
        spawnPosition = transform.position;
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            player = p.transform;

        PickRandomDirection();
    }

    void Update()
    {
        if (player == null)
        {
            // 念のため再取得を試す
            // player = GameObject.FindWithTag("Player");
            return;
        }

        if (isChasing)
        {
            UpdateChase();
        }
        else
        {
            // まず視界判定
            if (CanSeePlayer())
            {
                isChasing = true;
                loseTimer = 0f;
            }
            else
            {
                UpdateIdle();
            }
        }
    }

    void UpdateIdle()
    {
        dirTimer -= Time.deltaTime;
        if (dirTimer <= 0f)
        {
            PickRandomDirection();
        }

        // スポーン位置から wanderRadius 以上遠ざからないように補正
        Vector3 toSpawn = spawnPosition - transform.position;
        if (toSpawn.magnitude > wanderRadius)
        {
            currentDir = toSpawn.normalized;
        }

        Move(currentDir, idleSpeed);
    }

    void UpdateChase()
    {
        if (CanSeePlayer())
        {
            // 見えている間は常に追跡
            loseTimer = 0f;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Move(dirToPlayer, chaseSpeed);
        }
        else
        {
            // 視界から外れたらカウント開始
            loseTimer += Time.deltaTime;
            if (loseTimer >= loseSightTime)
            {
                isChasing = false;
                loseTimer = 0f;
                PickRandomDirection();
            }
            else
            {
                // まだ少しだけ最後に向かっていた方向に進む
                Move(currentDir, chaseSpeed * 0.5f);
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > viewRadius)
            return false;

        Vector2 dirToPlayer = toPlayer.normalized;

        // 敵の「正面」を currentDir で考える（止まっていたら右方向）
        Vector2 forward = currentDir.sqrMagnitude > 0.01f ? currentDir.normalized : Vector2.right;

        float angle = Vector2.Angle(forward, dirToPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        // 今回は「壁で遮られているか」は無視（レイキャスト無し）
        return true;
    }

    void Move(Vector2 dir, float speed)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        currentDir = dir.normalized;
        transform.position += (Vector3)(currentDir * speed * Time.deltaTime);

        // 見た目を進行方向に向けたい場合（任意）
        // transform.right = currentDir;
    }

    void PickRandomDirection()
    {
        // ランダムな方向ベクトル
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        currentDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        dirTimer = directionChangeInterval;
    }
}
