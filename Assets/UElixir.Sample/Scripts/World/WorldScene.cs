using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UElixir.Sample
{
    public class WorldScene : MonoBehaviour
    {
        [SerializeField]
        private NetworkEntity m_playerPrefab;
        [SerializeField]
        private Transform m_playerStart;
        [SerializeField]
        private UnitController m_unitController;
        [SerializeField]
        private CameraBoom m_mainCameraBoom;

        private void Start()
        {
            Assert.IsNotNull(NetworkManager.Instance);

            NetworkManager.Instance.SpawnLocalEntity(m_playerPrefab, OnPlayerSpawned);
        }

        private void OnPlayerSpawned(NetworkEntity entity, bool isSuccess)
        {
            if (isSuccess)
            {
                entity.transform.position = m_playerStart.position;
                m_unitController.ControlledUnit = entity.GetComponent<Movable>();
                m_mainCameraBoom.Target = entity.transform;
            }
        }
    }
}