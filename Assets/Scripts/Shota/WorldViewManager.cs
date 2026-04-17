using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class WorldViewManager : MonoBehaviour
{
    [Header("")]
    public TextMeshProUGUI boardText;

    [Header("")]
    public TextMeshProUGUI titleText;

    [Header("")]
    public Button nextButton;

    [Header("")]
    public TextMeshProUGUI buttonText;

    [Header("")]
    public LocalizedText localizedText;

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;

    [Header("")]
    public AudioSource ambientSource;
    public AudioClip ambientClip;

    // ���݉����ڂ̊Ŕ�\�����Ă��邩
    private int currentIndex = 0;

    // �Ŕ͑S����4���i���E��3���{�������1���j
    private int totalBoards = 4;

    // �e�Ŕ̃^�C�g���i���{��j
    private string[] japaneseTitles = {
        "�o�q�̗�}�t",
        "��̖͂@��",
        "����̈˗�",
        "�������"
    };

    // �e�Ŕ̃^�C�g���i�p��j
    private string[] englishTitles = {
        "Twin Mediums",
        "Law of Spiritual Power",
        "The Mission",
        "Controls"
    };

    void Start()
    {
        // BGM���Đ�����
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // �������Đ�����
        if (ambientSource != null && ambientClip != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        // �ŏ��̊Ŕ�\������
        ShowBoard(currentIndex);
    }

    // �Ŕ�\�����郁�\�b�h
    void ShowBoard(int index)
    {
        bool isJapanese = PlayerPrefs.GetString("Language", "JP") == "JP";

        // �{���e�L�X�g���X�V����
        if (localizedText != null && boardText != null)
        {
            boardText.text = localizedText.Get(index);
        }

        // �^�C�g���e�L�X�g���X�V����
        if (titleText != null)
        {
            if (isJapanese)
            {
                titleText.text = japaneseTitles[index];
            }
            else
            {
                titleText.text = englishTitles[index];
            }
        }

        // �Ō�̊ŔȂ�{�^���̃e�L�X�g���u����v�ɕς���
        if (buttonText != null)
        {
            if (index >= totalBoards - 1)
            {
                buttonText.text = isJapanese ? "����" : "Close";
            }
            else
            {
                buttonText.text = isJapanese ? "����" : "Next";
            }
        }
    }

    // �{�^�����������Ƃ��ɌĂ΂�郁�\�b�h
    public void OnNextButtonClicked()
    {
        currentIndex++;

        // �܂��Ŕ��c���Ă���Ȃ玟�̊Ŕ�\��
        if (currentIndex < totalBoards)
        {
            ShowBoard(currentIndex);
        }
        else
        {
            // �S���̊Ŕ����I�������TutorialScene�ֈړ�
            SceneManager.LoadScene("TutorialScene");
            //NetworkSceneManager.
        }
    }
}
