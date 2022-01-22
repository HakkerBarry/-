using System.Collections;
using System.Collections.Generic;
using UnityEngine;



    [RequireComponent(typeof(LineRenderer))]
    public class Electro : MonoBehaviour
    {

        public int m_ShakeInternal = 5;   //�𶯵�ʱ����
        private int m_shakeframeCount = 1;

        [SerializeField]
        private float m_noiseRange = 2;  //�벨���ţ�ԽС��Խƽ��
        public int m_MaxLineCount = 30;  //���ֶΣ��ֶ�Խ�࣬�����Խ��
        public int m_MaxnoiseRange = 1;  //һ��Ϊ1�Ϳ����ˣ��������
        /// <summary>
        /// ����ڵ�����
        /// </summary>
        private int m_linePointcount = 10;  
        /// <summary>
        /// ����ڵ����
        /// </summary>
        [SerializeField]
        private float m_pointDis = 0.5f;  //����
        [SerializeField]
        private Transform m_targetts;   //����Ķ˵㣬����Ը�publicȻ�����ö˵�
        private LineRenderer m_linerender;  //����Ⱦ��
        [SerializeField]
        private bool m_isLighting = false;  //�����Ƿ������磬������ʱ�ر�m_isLighting�Ļ�ֻ����ͣ��������ʧ����ʧ���ǿ�Gameobj��Trigger��ʵ��




    // Start is called before the first frame update
    void Start()
        {
            m_linerender = this.GetComponent<LineRenderer>();
        }

        public void StartLight(Transform targetts)
        {
            m_targetts = targetts;
            this.m_isLighting = true;
            this.m_shakeframeCount = m_ShakeInternal;
        }



        // Update is called once per frame
        void Update()
        {
            if (this.m_shakeframeCount > 0)
            {
                m_shakeframeCount--;
                return;
            }
            if (!this.m_isLighting) return;
            this.m_shakeframeCount = m_ShakeInternal;//���Խ��Խ��
            float distance = Vector3.Distance(transform.position, m_targetts.position);  //���GameObj�ĵ㵽Ŀ���ľ���
            int pointcount = Mathf.CeilToInt(distance / this.m_pointDis);  //����/����=ʵ�ʵ�����
            this.m_linePointcount = pointcount > this.m_MaxLineCount ? this.m_MaxLineCount : pointcount;  //�ж����޳�������
            if (this.m_linePointcount >= this.m_MaxLineCount)
                m_pointDis = distance / this.m_MaxLineCount;  //���������¼������
            this.m_linerender.positionCount = this.m_linePointcount + 1; //��������Ⱦ��Ĭ����11������
            Vector3 dir = (this.m_targetts.position - transform.position).normalized;  //���GameObj�ĵ㵽Ŀ���ķ�������
            for (int i = 0; i < this.m_linePointcount; i++)  //�������е�
            {    
                Vector3 pos = this.transform.position + dir * m_pointDis * i; //ȡ��ԭʼ���λ��
                float newnoiseRange = this.m_noiseRange * distance;  //��ƫ�Ʒ�Χ�;����
                if (newnoiseRange > this.m_MaxnoiseRange) newnoiseRange = this.m_MaxnoiseRange; //�ж����޳������Χ
                pos.x += Random.Range(-newnoiseRange, newnoiseRange); //ƫ��x
                pos.y += Random.Range(-newnoiseRange, newnoiseRange); //ƫ��y
                this.m_linerender.SetPosition(i, pos);  //������ֵ
            }
            this.m_linerender.SetPosition(this.m_linerender.positionCount - 1, this.m_targetts.position);  //�����һ���㣨Ĭ�ϵ�ʮ���㣩��ԭ��Ŀ���
        }
    }
