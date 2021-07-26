const express = require('express');
const router = express.Router();

const Room = require('../models/room');
const Passage = require('../models/passage');
const Aoi = require('../models/aoi');

//router to get the configuration page
router.get('/', async (req,res) => {
    const rooms = await Room.find();
    const aois = await Aoi.find();
    const passages = await Passage.find();

    res.render('config', {rooms: rooms, aois: aois, passages: passages});
})

router.post('/room', async (req,res)=>{
    console.log(req.body)

    //delete room if exists. not exactly REST compliant but it's easier.
    await Room.deleteOne({roomNumber: req.body.roomID})

    try{
        let room = new Room({
            roomNumber: req.body.roomID,
            description: req.body.description,
            aoi: req.body.rss,
        });

        for(passage of req.body.pss){
            room.passages.push(passage)
        }

        await room.save();
        res.redirect("/config/");

    } catch(e){
        console.log(e);
        res.redirect("/config/");
    }

})

router.post('/passage', async (req,res)=>{
    //delete passage if exists. not exactly REST compliant but it's easier.
    await Passage.deleteOne({sensorID: req.body.sensorID})

    let passage = new Passage({
        sensorID: req.body.sensorID,
    });

    try{
        await passage.save();
        res.redirect("/config/")
    } catch(e) {
        res.send(e)
    }
})


router.post('/aoi', async (req,res)=>{
    //delete aoi if exists. not exactly REST compliant but it's easier.
    await Aoi.deleteOne({sensorID: req.body.sensorID})

    let aoi = new Aoi({
        sensorID: req.body.sensorID,
        description: req.body.description,
        observation: {
            time: Date.now(),
            inhabitants: null,
        }
    });

    try{
        await aoi.save();
        res.redirect("/config/")
    } catch(e) {
        res.send(e)
    }
})


//TESTAREA
//create hardcoded room schema
/*
var pss1 = new Passage({
    sensorID: "A111.1"
})
var pss2 = new Passage({
    sensorID: "A111.2"
})
var rss1 = new Aoi({
    sensorID: "A111.rss",
    description: "Area of interest: kaffeemaschine",
    observation: {
        time: Date.now(),
        inhabitants: [
            {x: 12.1212, y: 65.1245},
            {x: 10.0032, y: 72.1243}
        ]
    }
})

var room1 = new Room({
    roomNumber : "A111",
    description: "this room is just a test room. two entries, one Aoi",
    passages: [pss1, pss2]
})


try{
    pss1.save()
    pss2.save()
    rss1.save()
    room1.save()
} catch(e){
    console.log(e)
}

console.log(room1)
console.log(pss1)
console.log(pss2)
console.log(rss1)

*/

module.exports = router;