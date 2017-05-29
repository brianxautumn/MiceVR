using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BinaryGame : MonoBehaviour
{

    public float TargetLength;
    public float GreyLength;
    public float PresentationLength;
    public float TunnelWidth;
    public float TargetDepth;
    public float MazeHeight;

    private float wallWidth = 0.01f;

    [SerializeField]
    private GameObject mazeTarget;
    // Maze walls
    private GameObject tunnelBackWall;
    private GameObject presentationWallLeft;
    private GameObject presentationWallRight;
    private GameObject greyWallLeft;
    private GameObject greyWallRight;
    private GameObject targetInnerWallLeft;
    private GameObject targetInnerWallRight;
	private GameObject targetCapWallLeft;
	private GameObject targetCapWallRight;
	private GameObject targetOuterCapWallLeft;
	private GameObject targetOuterCapWallRight;
    private GameObject outerCapWall;
    private GameObject floor;

    [SerializeField]
    private Material presentationMaterial1;
	[SerializeField]
	private Material presentationMaterial2;
	[SerializeField]
	private Material greyMaterial;
	[SerializeField]
	private Material floorMaterial;


    // Use this for initialization
    void Start()
    {
        float sidePosition = TunnelWidth / 2.0f;
        float forkStart = PresentationLength + GreyLength;
        float mazeHeightOffset = MazeHeight / 2.0f;

        // Generate Maze Here
        tunnelBackWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tunnelBackWall.transform.SetParent(mazeTarget.transform);
        tunnelBackWall.transform.name = "TunnelBackWall";
        tunnelBackWall.transform.localScale = new Vector3(TunnelWidth, MazeHeight, wallWidth);
        tunnelBackWall.transform.localPosition = new Vector3(0, mazeHeightOffset, 0);

        presentationWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        presentationWallLeft.transform.SetParent(mazeTarget.transform);
        presentationWallLeft.transform.name = "PresentationWallLeft";
        presentationWallLeft.transform.localScale = new Vector3(PresentationLength, MazeHeight, wallWidth);
        presentationWallLeft.transform.rotation *= Quaternion.Euler(0, -90, 0);
        presentationWallLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, PresentationLength / 2.0f);
        presentationWallLeft.GetComponent<Renderer>().material = presentationMaterial1;

		presentationWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        presentationWallRight.transform.SetParent(mazeTarget.transform);
        presentationWallRight.transform.name = "PresentationWallRight";
        presentationWallRight.transform.localScale = new Vector3(PresentationLength, MazeHeight, wallWidth);
        presentationWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
        presentationWallRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, PresentationLength / 2.0f);
        presentationWallRight.GetComponent<Renderer>().material = presentationMaterial1;

        if(GreyLength > 0){
			greyWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
			greyWallLeft.transform.SetParent(mazeTarget.transform);
			greyWallLeft.transform.name = "GreyWallLeft";
            greyWallLeft.transform.localScale = new Vector3(GreyLength, MazeHeight, wallWidth);
			greyWallLeft.transform.rotation *= Quaternion.Euler(0, -90, 0);
			greyWallLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, GreyLength / 2.0f + PresentationLength);
            greyWallLeft.GetComponent<Renderer>().material = greyMaterial;

			greyWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
			greyWallRight.transform.SetParent(mazeTarget.transform);
			greyWallRight.transform.name = "GreyWallRight";
			greyWallRight.transform.localScale = new Vector3(GreyLength, MazeHeight, wallWidth);
			greyWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
			greyWallRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, GreyLength / 2.0f + PresentationLength);
			greyWallRight.GetComponent<Renderer>().material = greyMaterial;
		}

        targetInnerWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetInnerWallLeft.transform.SetParent(mazeTarget.transform);
        targetInnerWallLeft.transform.name = "TargetInnerWallLeft";
        targetInnerWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth / 2.0f, mazeHeightOffset, forkStart);
        targetInnerWallLeft.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);

		targetInnerWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
		targetInnerWallRight.transform.SetParent(mazeTarget.transform);
		targetInnerWallRight.transform.name = "TargetInnerWallLeft";
		targetInnerWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth / 2.0f, mazeHeightOffset, forkStart);
		targetInnerWallRight.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);

        targetCapWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
		targetCapWallLeft.transform.SetParent(mazeTarget.transform);
		targetCapWallLeft.transform.name = "TargetCapWallLeft";
        targetCapWallLeft.transform.rotation *= Quaternion.Euler(0, 90, 0);
        targetCapWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth, mazeHeightOffset, forkStart + TargetLength / 2.0f);
		targetCapWallLeft.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);
        targetCapWallLeft.GetComponent<Renderer>().material = presentationMaterial1;

		targetCapWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
		targetCapWallRight.transform.SetParent(mazeTarget.transform);
		targetCapWallRight.transform.name = "TargetCapWallRight";
		targetCapWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
		targetCapWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth, mazeHeightOffset, forkStart + TargetLength / 2.0f);
		targetCapWallRight.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);
        targetCapWallRight.GetComponent<Renderer>().material = presentationMaterial1;

        targetOuterCapWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
		targetOuterCapWallLeft.transform.SetParent(mazeTarget.transform);
		targetOuterCapWallLeft.transform.name = "TargetOuterCapWallLeft";
        targetOuterCapWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth / 2.0f, mazeHeightOffset, forkStart + TargetLength);
		targetOuterCapWallLeft.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);
        targetOuterCapWallLeft.GetComponent<Renderer>().material = presentationMaterial1;

		targetOuterCapWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
		targetOuterCapWallRight.transform.SetParent(mazeTarget.transform);
		targetOuterCapWallRight.transform.name = "TargetOuterCapWallLeft";
		targetOuterCapWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth / 2.0f, mazeHeightOffset, forkStart + TargetLength);
		targetOuterCapWallRight.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);
		targetOuterCapWallRight.GetComponent<Renderer>().material = presentationMaterial1;

        outerCapWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
		outerCapWall.transform.SetParent(mazeTarget.transform);
		outerCapWall.transform.name = "OuterCapWall";
		outerCapWall.transform.localScale = new Vector3(TunnelWidth, MazeHeight, wallWidth);
        outerCapWall.transform.localPosition = new Vector3(0.0f, mazeHeightOffset, forkStart + TargetLength);

        floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.transform.SetParent(mazeTarget.transform);
        floor.transform.name = "Floor";
        floor.transform.rotation *= Quaternion.Euler(90, 0, 0);
        float floorLength = TargetLength + PresentationLength + GreyLength;
        floor.transform.localScale = new Vector3(TunnelWidth + 2 * TargetDepth, floorLength, 1.0f);
        floor.transform.localPosition = new Vector3(0, 0, floorLength / 2.0f);
        floor.transform.GetComponent<Renderer>().material = floorMaterial;
    }

    // Update is called once per frame
    void Update()
    {

    }

}
