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
    //delete room if exists. not exactly REST compliant but it's easier.
    await Room.deleteOne({roomNumber: req.body.roomID});

    //fix for issue #3
    if(typeof req.body.pss == "string"){
        req.body.pss = [req.body.pss]
    }
    req.body.rss = req.body.rss || null;

    //create document for room and save
    try{
        let room = new Room({
            roomNumber: req.body.roomID,
            description: req.body.description,
            aoi: req.body.rss,
        });

        for(passage of req.body.pss){
            room.passages.push(passage);
        }

        await room.save();
        res.redirect("/config/");

    } catch(e){
        console.log(e);
        res.redirect("/config/");
    }

});

router.post('/passage', async (req,res)=>{
    //delete passage if exists.
    await Passage.deleteOne({sensorID: req.body.sensorID});

    let passage = new Passage({
        sensorID: req.body.sensorID,
    });

    try{
        await passage.save();
        res.redirect("/config/");
    } catch(e) {
        res.send(e);
    }
});


router.post('/aoi', async (req,res)=>{
    //delete aoi if exists.
    await Aoi.deleteOne({sensorID: req.body.sensorID});

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
        res.redirect("/config/");
    } catch(e) {
        res.send(e);
    }
});

module.exports = router;