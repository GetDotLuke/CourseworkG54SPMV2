﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Realtime.Messaging.Internal;
using System;


public class World : MonoBehaviour {

	public GameObject player;
	public Material textureAtlas;
    public Material fluidTexture;
    public static int columnHeight = 16;
	public static int chunkSize = 16;
	public static int worldSize = 1;
	public static int radius = 5;
	public static uint maxCoroutines = 5000;
	public static ConcurrentDictionary<string, Chunk> chunks;
	public static List<string> toRemove = new List<string>();
	
	public static bool firstbuild = true;

	public Text timerText;
	float startTime;



	CoroutineQueue queue;

    //record last player position
    public Vector3 lastbuildPos;

    //takes position of chunk and generates a string from its xyz coordinates
	public static string BuildChunkName(Vector3 v)
	{
		return (int)v.x + "_" + 
			        (int)v.y + "_" + 
			        (int)v.z;
	}

	public static string BuildColumnName(Vector3 v)
	{
		return (int)v.x + "_" + (int)v.z;
	}

    //allows us to interact with blocks in chunks outside of where the player is.
    public static Block GetWorldBlock(Vector3 pos)
    {
        int cx, cy, cz;

        if (pos.x < 0)
            cx = (int)(Mathf.Round(pos.x - chunkSize) / (float)chunkSize) * chunkSize;
        else
            cx = (int)(Mathf.Round(pos.x) / (float)chunkSize) * chunkSize;

        if (pos.y < 0)
            cy = (int)(Mathf.Round(pos.y - chunkSize) / (float)chunkSize) * chunkSize;
        else
            cy = (int)(Mathf.Round(pos.y) / (float)chunkSize) * chunkSize;


        if (pos.z < 0)
            cz = (int)(Mathf.Round(pos.z - chunkSize) / (float)chunkSize) * chunkSize;
        else
            cz = (int)(Mathf.Round(pos.z) / (float)chunkSize) * chunkSize;

        int blx = (int)Mathf.Abs((float)Math.Round(pos.x) - cx);
        int bly = (int)Mathf.Abs((float)Math.Round(pos.y) - cy);
        int blz = (int)Mathf.Abs((float)Math.Round(pos.z) - cz);

        string cn = BuildChunkName(new Vector3(cx, cy, cz));
        Chunk c;
        if (chunks.TryGetValue(cn, out c))
        {

            return c.chunkData[blx, bly, blz];
        }
        else
            return null;
    }

    void BuildChunkAt(int x, int y, int z)
	{
		Vector3 chunkPosition = new Vector3(x*chunkSize, 
			                                y*chunkSize, 
			                                z*chunkSize);

		string n = BuildChunkName(chunkPosition);
		Chunk c;

		if(!chunks.TryGetValue(n, out c))
		{
            c = new Chunk(chunkPosition, textureAtlas, fluidTexture);
            c.chunk.transform.parent = this.transform;
            c.fluid.transform.parent = this.transform;
            chunks.TryAdd(c.chunk.name, c);
        }
	}

    //recursive 3d floodfill algorithm for world building
	IEnumerator BuildRecursiveWorld(int x, int y, int z, int startrad, int rad)
	{
        //when radius reachs 0 then break
        int nextrad = rad-1;
		if(rad <= 0 || y < 0 || y > columnHeight) yield break;
		//build chunk front
		BuildChunkAt(x,y,z+1);
		queue.Run(BuildRecursiveWorld(x,y,z+1,rad,nextrad));
		yield return null;

		//build chunk back
		BuildChunkAt(x,y,z-1);
		queue.Run(BuildRecursiveWorld(x,y,z-1,rad,nextrad));
		yield return null;

		//build chunk left
		BuildChunkAt(x-1,y,z);
		queue.Run(BuildRecursiveWorld(x-1,y,z,rad,nextrad));
		yield return null;

		//build chunk right
		BuildChunkAt(x+1,y,z);
		queue.Run(BuildRecursiveWorld(x+1,y,z,rad,nextrad));
		yield return null;

		//build chunk up
		BuildChunkAt(x,y+1,z);
		queue.Run(BuildRecursiveWorld(x,y+1,z,rad,nextrad));
		yield return null;

		//build chunk down
		BuildChunkAt(x,y-1,z);
		queue.Run(BuildRecursiveWorld(x,y-1,z,rad,nextrad));
		yield return null;

	}

    //loops through dictionary and draws chunks if they are set to draw
	IEnumerator DrawChunks()
	{
		toRemove.Clear();
		foreach(KeyValuePair<string, Chunk> c in chunks)
		{
			if(c.Value.status == Chunk.ChunkStatus.DRAW) 
			{
				c.Value.DrawChunk();
			}
			if(c.Value.chunk && Vector3.Distance(player.transform.position,
				c.Value.chunk.transform.position) > radius*chunkSize)
				toRemove.Add(c.Key);

			yield return null;
		}
	}

    //loops through chunks added to toRemove list, gets chunks keys, uses those to destroy chunks found in the dictionary but saves chunk to a file before doing so
	IEnumerator RemoveOldChunks()
	{
		for(int i = 0; i < toRemove.Count; i++)
		{
			string n = toRemove[i];
			Chunk c;
			if(chunks.TryGetValue(n, out c))
			{
				Destroy(c.chunk);
				c.Save();
				chunks.TryRemove(n, out c);
				yield return null;
			}
		}
	}

	public void BuildNearPlayer()
	{
        //stops any current chunk building and then restarts using new player position
		StopCoroutine("BuildRecursiveWorld");
		queue.Run(BuildRecursiveWorld((int)(player.transform.position.x/chunkSize),
			                                (int)(player.transform.position.y/chunkSize),
			                                (int)(player.transform.position.z/chunkSize),radius,radius));
	}

	// Use this for initialisation
	void Start () {
		Vector3 ppos = player.transform.position;
		player.transform.position = new Vector3(ppos.x,
			                                Utils.GenerateHeight(ppos.x,ppos.z) + 1,
			                                ppos.z);
		lastbuildPos = player.transform.position;
		player.SetActive(false);

		firstbuild = true;
		chunks = new ConcurrentDictionary<string, Chunk>();
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;	

		queue = new CoroutineQueue(maxCoroutines, StartCoroutine);
		startTime = Time.time;
		Debug.Log("Start Build");

		//build starting chunk
		BuildChunkAt((int)(player.transform.position.x/chunkSize),
			                                (int)(player.transform.position.y/chunkSize),
			                                (int)(player.transform.position.z/chunkSize));
		
        //draw it
		queue.Run(DrawChunks());

		//create a bigger world
		queue.Run(BuildRecursiveWorld((int)(player.transform.position.x/chunkSize),
			                                (int)(player.transform.position.y/chunkSize),
			                                (int)(player.transform.position.z/chunkSize),radius,radius));

		startTime = Time.time;
	}

	// Update is called once per frame
	void Update () {
		Vector3 movement = lastbuildPos - player.transform.position;

        //detects if player has moved from one chunk to another and sets new player position
		if(movement.magnitude > chunkSize )
		{
			lastbuildPos = player.transform.position;
			BuildNearPlayer();
		}

		if(!player.activeSelf)
		{
			player.SetActive(true);	
			Debug.Log("Built in " + (Time.time - startTime));
			firstbuild = false;
		}

		queue.Run(DrawChunks());
		queue.Run(RemoveOldChunks());


		float t = Time.time - startTime;

		string minutes = ((int)t / 60).ToString ();
		string seconds = (t % 60).ToString ();

		timerText.text = minutes + ":" + seconds;
	}
}
