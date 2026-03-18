using Godot;
using System;

public partial class PongLogic : Node
{
    [Export]
    public Node3D leftPaddle;

    [Export]
    public Node3D rightPaddle;

    [Export]
    public Node3D leftGhost;
    private AnimationPlayer leftAnim;

    [Export]
    public Node3D rightGhost;
    private AnimationPlayer rightAnim;

    [Export]
    public Camera3D cam;


    private bool leMagik;
    [Export]
    public Node3D map;
    [Export]
    public Node3D ball;

    [Export]
    public Vector2 tableSize;

    private Vector3 ballVelocity = Vector3.Zero;

    [Export]
    private float defaultBallSpeed = 5.0f;
    private float ballSpeed;

    [Export]
    private float defaultPaddleSpeed = 10.0f;
    private float paddleSpeed;

    [Export]
    public float defaultPaddleLerpSpeed = 20f;
    public float paddleLerpSpeed;

    private int gaemState;
    private float slowmo = 1f;
    [Export]
    public Timer timer;

    private Random random = new Random();

    public float leftStickMagnitude = 0;
    public float rightStickMagnitude = 0;
    public Vector2 leftStickInput = Vector2.Zero;
    public Vector2 rightStickInput = Vector2.Zero;

	public int sideCheck = 0;

    private float leftPaddleVerticalVelocity = 0;
    private float rightPaddleVerticalVelocity = 0;

    public override void _Ready()
    {
        ballSpeed = defaultBallSpeed;
        paddleSpeed = defaultPaddleSpeed;
        paddleLerpSpeed = defaultPaddleLerpSpeed;

        leftAnim = leftGhost.GetNode<AnimationPlayer>("Ghost/AnimationPlayer");
        rightAnim = rightGhost.GetNode<AnimationPlayer>("Ghost/AnimationPlayer");

        leMagik = false;
        ToggleLeMagik();
        InitMatch();

        leftAnim.AnimationFinished += (StringName animName) =>
        {
            if (animName == "Kick")
            {
                leftAnim.Play("Floaty");
            }
        };

        rightAnim.AnimationFinished += (StringName animName) =>
        {
            if (animName == "Kick")
            {
                rightAnim.Play("Floaty");
            }
        };
    }

    public override void _Process(double delta)
    {
        Vector3 camDefault = new Vector3(0, 1.521f, 1.6f);

        ballSpeed = defaultBallSpeed * slowmo;
        paddleSpeed = defaultPaddleSpeed * slowmo;
        paddleLerpSpeed = defaultPaddleLerpSpeed * slowmo;

        switch (gaemState)
        {
            case 0: // Before Match
                cam.Position = camDefault;
                ball.Position = new Vector3(0, -5, 0);
                gaemState = 1;

            break;

            case 1: // Cutscene
                
                cam.Position = cam.Position.Slerp(new Vector3(0, 0.65f, 0.5f), (float)delta * 3);


                if (ball.Position.Y < -0.01f)
                {
                    ball.Position = ball.Position.Slerp(new Vector3(0, 0, 0), (float)delta * 3);
                }
                else 
                {
                    ball.Position = Vector3.Zero;
                    gaemState = 2;
                }

            break;

            case 2:
                if (cam.Position.DistanceTo(camDefault) > 0.01f)
                {
                    cam.Position = cam.Position.Slerp(camDefault, (float)delta * 3);
                }
                else
                {
                    cam.Position = camDefault;
                    gaemState = 3;
                }
            break;

            case 3:
                PollInput((float)delta);
                PaddleMovement((float)delta);
                BallMovement((float)delta);
                CheckPaddleCollision();
                CheckForScore();
                cam.Position = camDefault.Slerp(new Vector3(ball.Position.X, camDefault.Y, camDefault.Z), 0.01f);
            break;


            case 4:
                PollInput((float)delta);
                PaddleMovement((float)delta);
                BallMovement((float)delta);
                CheckPaddleCollision();
                CheckForScore();
                rightPaddle.Position = new Vector3(rightPaddle.Position.X, rightPaddle.Position.Y, ball.Position.Z);
                if (slowmo > 0.2f)
                {
                    cam.Position = cam.Position.Slerp(new Vector3(ball.Position.X, ball.Position.Y + 0.65f, ball.Position.Z + 0.5f), (float)delta * 10);
                    slowmo -= 0.1f;
                    Engine.TimeScale = slowmo;
                }
                else
                {
                    timer.Start();
                    slowmo = 0.1f;
                    Engine.TimeScale = slowmo;
                    gaemState = 6;
                }
            break;

            case 5:
                PollInput((float)delta);
                PaddleMovement((float)delta);
                BallMovement((float)delta);
                CheckPaddleCollision();
                CheckForScore();
                leftPaddle.Position = new Vector3(leftPaddle.Position.X, leftPaddle.Position.Y, ball.Position.Z);
                if (slowmo > 0.2f)
                {
                    cam.Position = cam.Position.Slerp(new Vector3(ball.Position.X, ball.Position.Y + 0.65f, ball.Position.Z + 0.5f), (float)delta * 10);
                    slowmo -= 0.1f;
                    Engine.TimeScale = slowmo;
                }
                else
                {
                    timer.Start();
                    slowmo = 0.1f;
                    Engine.TimeScale = slowmo;
                    gaemState = 6;
                }
            break;

            case 6:
                if (timer.IsStopped())
                {
                    gaemState = 7;
                    ball.GetNode<AudioStreamPlayer>("Swoosh").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
                    ball.GetNode<AudioStreamPlayer>("Swoosh").Play();
                    ball.GetNode<AudioStreamPlayer>("Wow").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
                    ball.GetNode<AudioStreamPlayer>("Wow").Play();
                }
            break;

            case 7:
                PollInput((float)delta);
                PaddleMovement((float)delta);
                BallMovement((float)delta);
                CheckPaddleCollision();
                CheckForScore();
                cam.Position = cam.Position.Slerp(camDefault.Slerp(new Vector3(ball.Position.X, camDefault.Y, camDefault.Z), 0.01f), (float)delta * 3);
                if (slowmo < 1f)
                {
                    slowmo += 0.1f;
                    Engine.TimeScale = slowmo;
                }
                else
                {
                    slowmo = 1f;
                    Engine.TimeScale = slowmo;
                    gaemState = 3;
                }
                break;
        }
        


        if (Input.IsActionJustPressed("Toggle"))
        {
            ToggleLeMagik();
        }

        
        
        
        

        /*
        if (leftGhost.Position.DistanceTo(ball.Position) < 0.15f)
        {
            leftAnim.Play("Kick");
        }
        else if (rightGhost.Position.DistanceTo(ball.Position) < 0.15f)
        {
            rightAnim.Play("Kick");
        }*/
    }

    // Ball movement with speed adjustments
    public void BallMovement(float delta)
    {
        ball.Translate(ballVelocity * delta * slowmo);
        bool outOfBoundsTop = ball.Position.Z > tableSize.Y / 2.0f;
        bool outOfBoundsBottom = ball.Position.Z < -tableSize.Y / 2.0f;
        if (outOfBoundsTop && ballVelocity.Z > 0.0f || outOfBoundsBottom && ballVelocity.Z < 0.0f)
        {
            ballVelocity.Z *= -1;
            ball.GetNode<AudioStreamPlayer>("Bonk").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
            ball.GetNode<AudioStreamPlayer>("Bonk").Play();
        }
    }

    // Paddle movement
    public void PaddleMovement(float delta)
    {
        Vector3 leftPaddlePosition = leftPaddle.Position;
        leftPaddlePosition.Z += leftStickInput.Y * paddleSpeed * leftStickMagnitude * delta;
        leftPaddlePosition.Z = Mathf.Clamp(leftPaddlePosition.Z, (-tableSize.Y + leftPaddle.Scale.Z) / 2, (tableSize.Y - leftPaddle.Scale.Z) / 2);
        leftPaddleVerticalVelocity = (leftPaddlePosition - leftPaddle.Position).Length();
        leftPaddle.Position = leftPaddlePosition;

        Vector3 rightPaddlePosition = rightPaddle.Position;
        rightPaddlePosition.Z += rightStickInput.Y * paddleSpeed * rightStickMagnitude * delta;
        rightPaddlePosition.Z = Mathf.Clamp(rightPaddlePosition.Z, (-tableSize.Y + rightPaddle.Scale.Z) / 2, (tableSize.Y - rightPaddle.Scale.Z) / 2);
        rightPaddleVerticalVelocity = (rightPaddlePosition - rightPaddle.Position).Length();
        rightPaddle.Position = rightPaddlePosition;

        leftGhost.Position = new Vector3(leftPaddle.Position.X, 0f, leftPaddle.Position.Z);
        rightGhost.Position = new Vector3(rightPaddle.Position.X, 0f, rightPaddle.Position.Z);
    }

    // Initialize match and set ball starting velocity
    public void InitMatch()
    {
        ball.GlobalPosition = new Vector3(0, -5f, 0);
        float angle = Mathf.DegToRad(random.Next(-45, 45));
        int horizontalDirection = random.Next(0, 2) == 0 ? 1 : -1;
        float velocityX = horizontalDirection * Mathf.Cos(angle);
        float velocityZ = Mathf.Sin(angle);
        ballVelocity = new Vector3(velocityX, 0, velocityZ) * ballSpeed;

        leftAnim.Play("Floaty");
        rightAnim.Play("Floaty");
    }

    // Restart match
    public void LooseMatch()
    {
        ball.GetNode<AudioStreamPlayer>("Miss").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
        ball.GetNode<AudioStreamPlayer>("Miss").Play();
        gaemState = 0;
        InitMatch();
    }

    // Handle joystick input for paddles (Same joystick)
    public void PollInput(float delta)
    {
        float leftX = Input.GetJoyAxis(0, JoyAxis.LeftX);
        float leftY = Input.GetJoyAxis(0, JoyAxis.LeftY);
        leftStickMagnitude = new Vector2(leftX, leftY).Length();
        leftStickInput = new Vector2(leftX, leftY);
        if (leftStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            leftStickInput = Vector2.Zero;
        }

        float rightX = Input.GetJoyAxis(0, JoyAxis.RightX);
        float rightY = Input.GetJoyAxis(0, JoyAxis.RightY);
        rightStickMagnitude = new Vector2(rightX, rightY).Length();
        rightStickInput = new Vector2(rightX, rightY);

        if (rightStickMagnitude < 0.04f) // Fuzzy joystick setting..
        {
            rightStickInput = Vector2.Zero;
        }
    }

 // Check paddle collision with the ball
    private void CheckPaddleCollision()
    {
        Node3D targetPaddle = ballVelocity.X < 0 ? leftPaddle : rightPaddle;
        float paddleHalfSizeZ = targetPaddle.Scale.Z / 2.0f;
        float paddleCenterZ = targetPaddle.GlobalPosition.Z;
        float paddleMinZ = paddleCenterZ - paddleHalfSizeZ;
        float paddleMaxZ = paddleCenterZ + paddleHalfSizeZ;

        if (Mathf.Abs(ball.GlobalPosition.X - targetPaddle.GlobalPosition.X) < targetPaddle.Scale.X / 2.0f)
        {
            if (ball.GlobalPosition.Z >= paddleMinZ && ball.GlobalPosition.Z <= paddleMaxZ)
            {
                ballVelocity.X *= -1;
                float distanceFromCenter = ball.GlobalPosition.Z - paddleCenterZ;
                float maxAngle = 75.0f;  
                float angle = Mathf.DegToRad(maxAngle * (distanceFromCenter / paddleHalfSizeZ));

                ballVelocity.Z = Mathf.Sin(angle) * ballSpeed;
                ballVelocity = ballVelocity.Normalized() * ballSpeed;

			    if(leftPaddleVerticalVelocity > 0.07f && targetPaddle == leftPaddle)
                {
				    ballVelocity = ballVelocity * 2.0f;
                    leftPaddle.Position = new Vector3(leftPaddle.Position.X, leftPaddle.Position.Y, ball.Position.Z);
                    EpicShot(false);
                }
                else if (targetPaddle == leftPaddle)
                {
                    ball.GetNode<AudioStreamPlayer>("Hit").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
                    ball.GetNode<AudioStreamPlayer>("Hit").Play();
                }
                if (rightPaddleVerticalVelocity > 0.07f && targetPaddle == rightPaddle)
                {
                    ballVelocity = ballVelocity * 2.0f;
                    rightPaddle.Position = new Vector3(rightPaddle.Position.X, rightPaddle.Position.Y, ball.Position.Z);
                    EpicShot(true);
                }
                else if (targetPaddle == rightPaddle)
                {
                    ball.GetNode<AudioStreamPlayer>("Hit").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
                    ball.GetNode<AudioStreamPlayer>("Hit").Play();
                }

                if (ball.GlobalPosition.X < targetPaddle.GlobalPosition.X)
                {
                    ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X - targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
                }
                else
                {
                    ball.GlobalPosition = new Vector3(targetPaddle.GlobalPosition.X + targetPaddle.Scale.X / 2, ball.GlobalPosition.Y, ball.GlobalPosition.Z);
                }

                if(targetPaddle == leftPaddle && leftAnim.CurrentAnimation == "Floaty")
                {
                    leftAnim.Play("Kick");
                }
                if (targetPaddle == rightPaddle && rightAnim.CurrentAnimation == "Floaty")
                {
                    rightAnim.Play("Kick");
                }

            }
        }
    }

    // Check if the ball goes out of bounds for scoring
    private void CheckForScore()
    {
        float padding = 2f;
        if (ball.GlobalPosition.X < -tableSize.X / 2 - padding || ball.GlobalPosition.X > tableSize.X / 2 + padding)
        {
            LooseMatch();
        }
    }

    private void ToggleLeMagik()
    {
        if (leMagik)
        {
            leMagik = false;
            leftGhost.Visible = false;
            rightGhost.Visible = false;
            map.GetNode<Node3D>("Map").Visible = false;
            ball.GetNode<MeshInstance3D>("FancyBall").Visible = false;

            ball.GetNode<MeshInstance3D>("Ball").Visible = true;
            map.GetNode<Node3D>("Centerline").Visible = true;
            leftPaddle.GetNode<MeshInstance3D>("LeftPaddleMesh").Visible = true;
            rightPaddle.GetNode<MeshInstance3D>("LeftPaddleMesh").Visible = true;

        }
        else
        {
            leMagik = true;
            leftGhost.Visible = true;
            rightGhost.Visible = true;
            map.GetNode<Node3D>("Map").Visible = true;
            ball.GetNode<MeshInstance3D>("FancyBall").Visible = true;

            ball.GetNode<MeshInstance3D>("Ball").Visible = false;
            map.GetNode<Node3D>("Centerline").Visible = false;
            leftPaddle.GetNode<MeshInstance3D>("LeftPaddleMesh").Visible = false;
            rightPaddle.GetNode<MeshInstance3D>("LeftPaddleMesh").Visible = false;

        }
    }

    private void EpicShot(bool right)
    {
        ball.GetNode<AudioStreamPlayer>("Glass").PitchScale = (float)GD.RandRange(0.85f, 1.25f);
        ball.GetNode<AudioStreamPlayer>("Glass").Play();

        if (right)
        {
            gaemState = 4;
        }
        else
        {
            gaemState = 5;
        }
    }

}
