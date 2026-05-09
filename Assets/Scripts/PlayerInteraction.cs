using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button kickBtn;
    [SerializeField] private Button autoKickBtn;
    [SerializeField] private Button resetBtn;

    [Header("Settings")]
    [SerializeField] private float detectRange = 3f;
    [SerializeField] private float kickForce = 25f;
    [SerializeField] private GameObject confettiPrefab;

    private CameraController camScript;
    private Transform[] goals; // Lưu Transform thay vì GameObject để tối ưu bộ nhớ

    void Start()
    {
        camScript = Camera.main.GetComponent<CameraController>();

        // Tìm và lưu trực tiếp Transform của các khung thành ngay từ đầu
        GameObject[] goalObjs = GameObject.FindGameObjectsWithTag("Goal");
        goals = new Transform[goalObjs.Length];
        for (int i = 0; i < goalObjs.Length; i++)
        {
            goals[i] = goalObjs[i].transform;
        }

        // Dùng Lambda Expression để gộp code, bỏ bớt 2 hàm thừa (KickNearest và AutoKickFarthest)
        kickBtn.onClick.AddListener(() => Shoot(FindBall(true)));
        autoKickBtn.onClick.AddListener(() => Shoot(FindBall(false)));

        // Dùng buildIndex load scene sẽ nhanh và an toàn hơn dùng chuỗi string name
        resetBtn.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        kickBtn.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateKickButtonVisibility();
    }

    void UpdateKickButtonVisibility()
    {
        GameObject[] currentBalls = GameObject.FindGameObjectsWithTag("Ball");
        bool isNearBall = false;

        foreach (GameObject b in currentBalls)
        {
            if (b != null && Vector3.Distance(transform.position, b.transform.position) < detectRange)
            {
                isNearBall = true;
                break;
            }
        }

        // Cực kỳ quan trọng: Chỉ gọi hàm SetActive khi thực sự có sự thay đổi trạng thái
        // Tránh việc Unity phải vẽ lại UI liên tục mỗi khung hình (gây tụt FPS)
        if (kickBtn.gameObject.activeSelf != isNearBall)
        {
            kickBtn.gameObject.SetActive(isNearBall);
        }
    }

    GameObject FindBall(bool getNearest)
    {
        GameObject[] currentBalls = GameObject.FindGameObjectsWithTag("Ball");
        GameObject target = null;
        float bestDist = getNearest ? float.MaxValue : float.MinValue;

        foreach (GameObject b in currentBalls)
        {
            if (b == null) continue;

            float dist = Vector3.Distance(transform.position, b.transform.position);
            if ((getNearest && dist < bestDist) || (!getNearest && dist > bestDist))
            {
                bestDist = dist;
                target = b;
            }
        }
        return target;
    }

    void Shoot(GameObject ball)
    {
        if (ball == null) return;

        // Tìm gôn gần nhất
        Transform bestGoal = goals[0];
        if (goals.Length > 1 && Vector3.Distance(ball.transform.position, goals[1].position) <
                                Vector3.Distance(ball.transform.position, goals[0].position))
        {
            bestGoal = goals[1];
        }

        // Dùng TryGetComponent thay cho GetComponent (Chuẩn code Unity hiện đại, tránh lỗi Null)
        if (ball.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            Vector3 dir = (bestGoal.position - ball.transform.position).normalized;
            rb.AddForce(dir * kickForce + Vector3.up * 5f, ForceMode.Impulse);
        }

        StartCoroutine(HandleBallSequence(ball, bestGoal));
    }

    IEnumerator HandleBallSequence(GameObject ball, Transform goal)
    {
        camScript.target = ball.transform;

        float timer = 0;

        // Đã xóa biến hasScored thừa thãi vì lệnh break đã xử lý việc thoát vòng lặp
        while (timer < 5f && ball != null)
        {
            if (Vector3.Distance(ball.transform.position, goal.position) < 4f)
            {
                if (confettiPrefab) Instantiate(confettiPrefab, ball.transform.position, Quaternion.identity);

                yield return new WaitForSeconds(0.3f);
                if (ball != null) Destroy(ball);

                break; // Thoát vòng lặp ngay khi ghi bàn
            }

            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        camScript.target = this.transform;
    }
}