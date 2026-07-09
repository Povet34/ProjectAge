using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAge;

public class RagdollGrabTests
{
    readonly List<GameObject> _spawned = new List<GameObject>();

    GameObject Spawn(string name, Vector3 pos)
    {
        var go = RagdollFactory.CreateDummy(name, pos);
        _spawned.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TestGround";
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        _spawned.Add(ground);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _spawned)
            if (go != null) Object.Destroy(go);
        _spawned.Clear();
    }

    // Scope 1: ragdoll toggle flips the bone kinematic state.
    [UnityTest]
    public IEnumerator Ragdoll_Toggle_SwitchesKinematicState()
    {
        var rc = Spawn("Dummy", new Vector3(0f, 3f, 0f)).GetComponent<RagdollController>();

        Assert.IsFalse(rc.IsRagdoll, "starts non-ragdoll");
        foreach (var b in rc.Bodies) Assert.IsTrue(b.isKinematic, "bones kinematic when not ragdoll");

        rc.SetRagdoll(true);
        Assert.IsTrue(rc.IsRagdoll);
        foreach (var b in rc.Bodies) Assert.IsFalse(b.isKinematic, "bones dynamic when ragdoll");

        rc.SetRagdoll(false);
        foreach (var b in rc.Bodies) Assert.IsTrue(b.isKinematic, "bones kinematic again");

        yield return null;
    }

    // Scope 2: grabbing a character ragdolls it and joints it to the grabber's hand.
    [UnityTest]
    public IEnumerator Grab_MakesTargetRagdoll_AndCreatesJoint()
    {
        var grabber = Spawn("A", new Vector3(0f, 3f, 0f)).GetComponent<Grabber>();
        var brc = Spawn("B", new Vector3(0.5f, 3f, 0f)).GetComponent<RagdollController>();

        Assert.IsFalse(brc.IsRagdoll);

        grabber.Grab(Hand.Left, brc, brc.HipsBody);

        Assert.IsTrue(brc.IsRagdoll, "grabbed target becomes ragdoll");
        Assert.IsTrue(grabber.IsGrabbing(Hand.Left));
        Assert.IsNotNull(brc.HipsBody.GetComponent<SpringJoint>(), "joint created on grabbed body");

        grabber.Release(Hand.Left);
        yield return new WaitForFixedUpdate();

        Assert.IsFalse(grabber.IsGrabbing(Hand.Left), "released");
        Assert.IsNull(brc.HipsBody.GetComponent<SpringJoint>(), "joint removed on release");
    }

    // Scope 3: A -> B <- C. Two grabbers on one target must not explode or NaN.
    [UnityTest]
    public IEnumerator MultiGrab_TwoGrabbers_OneTarget_StaysStable()
    {
        var ga = Spawn("A", new Vector3(-2f, 3f, 0f)).GetComponent<Grabber>();
        var gc = Spawn("C", new Vector3(2f, 3f, 0f)).GetComponent<Grabber>();
        var brc = Spawn("B", new Vector3(0f, 3f, 0f)).GetComponent<RagdollController>();

        ga.Grab(Hand.Right, brc, brc.HipsBody);
        gc.Grab(Hand.Left, brc, brc.HipsBody);

        Assert.AreEqual(2, brc.HipsBody.GetComponents<SpringJoint>().Length,
            "target held by two joints (A -> B <- C)");
        Assert.IsTrue(brc.IsRagdoll);

        for (int i = 0; i < 130; i++) yield return new WaitForFixedUpdate();

        var p = brc.HipsBody.position;
        Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z),
            "no NaN (vector-zero tug did not blow up)");
        Assert.Less(p.magnitude, 50f, "target did not explode away");
        Assert.IsTrue(brc.IsRagdoll, "still ragdoll while held from both sides");
    }
}
