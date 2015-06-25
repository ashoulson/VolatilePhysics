using System;
using System.Collections.Generic;

using UnityEngine;

public class RollbackController : MonoBehaviour
{
  private class Command
  {
    public bool pollUp;
    public bool pollDown;
    public bool pollLeft;
    public bool pollRight;

    public Vector2 position;
    public float angle;

    public Vector2 velocity;
    public float angVelocity;

    public bool hasBeenExecuted;

    public Command(
      bool pollUp,
      bool pollDown,
      bool pollLeft,
      bool pollRight)
    {
      this.pollUp = pollUp;
      this.pollDown = pollDown;
      this.pollLeft = pollLeft;
      this.pollRight = pollRight;
      this.position = Vector2.zero;
      this.angle = 0.0f;
      this.hasBeenExecuted = false;
    }
  }

  private VolatileBody body;
  private bool pollUp;
  private bool pollDown;
  private bool pollLeft;
  private bool pollRight;

  private int debugIndex;
  private bool pause;

  private List<Command> commands;

  void Start()
  {
    this.body = this.GetComponent<VolatileBody>();
    this.commands = new List<Command>();

    Command start = new Command(false, false, false, false);
    start.position = this.body.body.Position;
    start.angle = this.body.body.Angle;
    start.velocity = this.body.body.LinearVelocity;
    start.angVelocity = this.body.body.AngularVelocity;
    this.commands.Add(start);
    this.debugIndex = 0;
  }

  void Update()
  {
    this.pollUp = Input.GetKey(KeyCode.UpArrow);
    this.pollDown = Input.GetKey(KeyCode.DownArrow);
    this.pollLeft = Input.GetKey(KeyCode.LeftArrow);
    this.pollRight = Input.GetKey(KeyCode.RightArrow);

    if (Input.GetKeyDown(KeyCode.D))
    {
      while (this.commands.Count > 10)
        this.commands.RemoveAt(0);
    }

    if (Input.GetKeyDown(KeyCode.Q))
      this.debugIndex--;
    if (Input.GetKeyDown(KeyCode.A))
      this.debugIndex++;

    if (this.debugIndex < 0)
      this.debugIndex = 0;
    if (this.debugIndex >= this.commands.Count)
      this.debugIndex = this.commands.Count - 1;

    if (Input.GetKeyDown(KeyCode.S))
      this.PrintDebug();

    if (Input.GetKeyDown(KeyCode.W))
      this.pause = !this.pause;
  }

  private Command ConstructCommand()
  {
    return new Command(
        this.pollUp,
        this.pollDown,
        this.pollLeft,
        this.pollRight);
  }

  void FixedUpdate()
  {
    if (this.body != null && this.pause == false)
    {
      Debug.Log("Starting");
      Command first = this.commands[0];
      this.body.body.SetWorld(first.position, first.angle);
      this.body.body.LinearVelocity = first.velocity;
      this.body.body.AngularVelocity = first.angVelocity;

      if (this.commands.Count < 60)
      {
        Command newCommand = ConstructCommand();
        this.commands.Add(newCommand);
      }

      foreach (Command cmd in this.commands)
      {
        if (cmd == first)
          continue;

        this.ApplyCommand(cmd);
        VolatileWorld.Instance.WorldUpdate();

        if (cmd.hasBeenExecuted == false)
        {
          Debug.Log("Storing");
          cmd.position = this.body.body.Position;
          cmd.angle = this.body.body.Angle;
          cmd.velocity = this.body.body.LinearVelocity;
          cmd.angVelocity = this.body.body.AngularVelocity;
          cmd.hasBeenExecuted = true;
        }
      }
    }
  }

  private void PrintDebug()
  {
    Command debug = this.commands[this.debugIndex];
    Debug.Log( 
      "Idx: " + this.debugIndex + "\n" +
      "Pos: " + debug.position.x + " " + debug.position.y + "\n" +
      "Vel: " + debug.velocity.x + " " + debug.velocity.y + "\n" +
      "Ang: " + debug.angle + "\n" +
      "AnV: " + debug.angVelocity);
  }

  void OnDrawGizmos()
  {
    if (this.commands != null)
    {
      //for(int i = 0; i < this.commands.Count; i++)
      //{
      //  Command cmd = this.commands[i];
      //  this.body.body.SetWorld(cmd.position, cmd.angle);
      //  Volatile.VolatileUtil.Draw(this.body.body);
      //}

      //Command debug = this.commands[this.debugIndex];
      //this.body.body.SetWorld(debug.position, debug.angle);
      //this.body.body.AABB.GizmoDraw(Color.white);
    }
  }

  private void ApplyCommand(Command cmd)
  {
    float thrust =
      (cmd.pollUp ? 1.0f : 0.0f) + (cmd.pollDown ? -1.0f : 0.0f);
    float turn =
      (cmd.pollLeft ? 1.0f : 0.0f) + (cmd.pollRight ? -1.0f : 0.0f);
    this.body.AddForce(this.body.body.Facing * thrust * 0.1f);
    this.body.AddTorque(-turn * 0.03f);

    // Stabilize
    float angVel = this.body.body.AngularVelocity;
    float inertia = this.body.body.Inertia;
    float correction =
      (angVel * inertia) / Time.fixedDeltaTime;

    float quotient = Mathf.Approximately(turn, 0.0f) ? 0.6f : 0.2f;
    this.body.AddTorque(correction * quotient);
  }
}
