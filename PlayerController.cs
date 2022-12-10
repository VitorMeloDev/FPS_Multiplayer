using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("View Player")]
    public Transform viewPoint;
    public float mouseSensitivy = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;
    public bool invertLook;
    private Camera cam;

    [Header("Move Player")]
    public float moveSpeed;
    public float runSpeed;
    private float activeSpeed;
    public float jumpMod;
    private Vector3 moveDir, movement;
    public CharacterController charaCon;
    public Transform groundTransformCheck;
    private bool isGrounded;
    public LayerMask layerMask; 
    public Animator myAnim;
    public GameObject playerModel;

    [Header("Shoot Player")]
    [SerializeField] private GameObject impactBullet,bulletPlayerImpact;
    //public float timeBetweenShots = .1f;
    private float shotCounter;
    public float maxHeat = 10f,/* heatPerShot = 1f */ coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    [Header("Guns")]
    public Gun[] allGuns;
    private int selectedGun = 0; 
    public float muzzleDisplayTime;
    private float muzzleCounter;
    public Transform modelGunPoint, gunHolder, adsInPoint, adsOutPoint;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public Material[] allSkins;
    private float adsSpeed = 5f;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        cam = Camera.main;
        //SwitchGun();
        photonView.RPC("GetGun", RpcTarget.All, selectedGun);
        UI_Controller.instance.weaponTemp.maxValue = maxHeat;
        currentHealth = maxHealth;

        if(photonView.IsMine)
        {
            UI_Controller.instance.healthSlider.maxValue = maxHealth;
            UI_Controller.instance.healthSlider.value = currentHealth;
            playerModel.SetActive(false);
            UI_Controller.instance.leaderboard.gameObject.SetActive(false);

        }else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
        //Transform trans = SpawManager.instance.GetSpawPoint();
        //transform.position = trans.position;
        //transform.rotation = trans.rotation;

        if(PhotonNetwork.CurrentRoom.MaxPlayers <= 8)
        {
            playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber];
        }
        else
        {
            playerModel.GetComponent<Renderer>().material = allSkins[Random.Range(0, allSkins.Length)];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(photonView.IsMine)
        {
            ViewPlayer();
            MovePlayer();  
            VereficaMouseNaTela();
            ScrollSwitchGun();

            if(allGuns[selectedGun].muzzle.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if(muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzle.SetActive(false);
                }
            }

            
            if(!overHeated)
            {
                if(!UI_Controller.instance.pauseScreen.activeInHierarchy)
                {
                    if(Input.GetMouseButtonDown(0))
                    {
                        Shoot();
                        allGuns[selectedGun].shotSound.Play();
                    }

                    if(Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                    {
                        shotCounter -= Time.deltaTime;

                        if(shotCounter <= 0)
                        {
                            Shoot();
                            allGuns[selectedGun].shotSound.Play();

                        }
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;
            }else
            {
                heatCounter -= overheatCoolRate * Time.deltaTime;
                if(heatCounter <= 0)
                {
                    overHeated = false;
                    UI_Controller.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            if(Input.GetMouseButton(1))
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView ,allGuns[selectedGun].adsValue, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView ,60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);

            }



            if(heatCounter < 0)
            {
                heatCounter = 0f;
            }
            UI_Controller.instance.weaponTemp.value = heatCounter;

            myAnim.SetBool("grounded", isGrounded);
            myAnim.SetFloat("speed", moveDir.magnitude);
        }

    }

    void LateUpdate()
    {
        if(photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position = viewPoint.transform.position;
                cam.transform.rotation = viewPoint.transform.rotation;
            }
            else
            {
                cam.transform.position = MatchManager.instance.mapCamPoint.transform.position;
                cam.transform.rotation = MatchManager.instance.mapCamPoint.transform.rotation;
            }
            
        }
       
    }

    void ViewPlayer()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivy;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);
    
        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f,60f); 

        if(invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y,viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y,viewPoint.rotation.eulerAngles.z);
        }

    }

    void MovePlayer()
    {
        if(Input.GetKey(KeyCode.LeftShift))
        {
            activeSpeed = runSpeed;
        }
        else
        {
            activeSpeed = moveSpeed;
        }
        
       
        float yVel = movement.y;

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"),0f,Input.GetAxisRaw("Vertical"));
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeSpeed;
        movement.y = yVel;
        movement.y += Physics.gravity.y * Time.deltaTime;
        charaCon.Move(movement * Time.deltaTime);

        isGrounded = Physics.Raycast(groundTransformCheck.position, Vector3.down, .25f, layerMask);

        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpMod;
        }
    }

    void VereficaMouseNaTela()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if(Cursor.lockState == CursorLockMode.None)
        {
            if(Input.GetMouseButtonDown(0) && !UI_Controller.instance.pauseScreen.activeInHierarchy)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f,.5f,0f));
        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("WE Hit" + hit.collider.gameObject.name);

            if(hit.collider.gameObject.tag == "Player")
            {
                PhotonNetwork.Instantiate(bulletPlayerImpact.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].gunDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject bullet = Instantiate(impactBullet, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bullet,10f);
            }

            
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;
            UI_Controller.instance.overheatedMessage.gameObject.SetActive(true);
        }
        
        allGuns[selectedGun].muzzle.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    [PunRPC]
    public void DealDamage(string damager, int amountDamage, int actor)
    {
        TakeDamage(damager,amountDamage,actor);
    }

    public void TakeDamage(string damager, int amountDamage, int actorValue)
    {
        if(photonView.IsMine)
        {
            currentHealth -= amountDamage;
            UI_Controller.instance.leaderboard.gameObject.SetActive(false);

            if(currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
                MatchManager.instance.UpadateStatSend(actorValue, 0, 1);
            }
            UI_Controller.instance.healthSlider.value = currentHealth;

        }
        
    }
    void ScrollSwitchGun()
    {
        if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;
            if(selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            //SwitchGun();
            photonView.RPC("GetGun", RpcTarget.All, selectedGun);
        }
        else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;
            if(selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            //SwitchGun();
            photonView.RPC("GetGun", RpcTarget.All, selectedGun);
        }

        for(int i = 0; i < allGuns.Length; i++)
        {
            if(Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                //SwitchGun();
                photonView.RPC("GetGun", RpcTarget.All, selectedGun);
            }
        }
    }

    void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
    }
    
    [PunRPC]
    public void GetGun(int gun)
    {
        if(gun < allGuns.Length)
        {
            selectedGun = gun;
            SwitchGun();
        }
    }
}
