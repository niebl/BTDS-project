const express = require('express');
const router = express.Router();

const Room = require('./../models/room');
const Passage = require('./../models/passage');
const Aoi = require('./../models/aoi');

//search for all types of sensors. allows SensorID and _id
router.get('/', async (req,res) => {
    let sensorID = req.query.sensorid || null;
    let _id = req.query.id || null;

    let query = {
    }

    if(sensorID != null){
        query.sensorID = sensorID;
    }
    if(_id != null){
        query._id = _id;
    }

    const rss = await Aoi.find(query);
    const pss = await Passage.find(query);
    
    const output = rss.concat(pss);
    
    res.send(output);
})

//This part here will do a lot of the inferring work
router.post('/pss/:sensorID', async (req,res) => {
    try{
        let pss = await Passage.find({sensorID: req.params.sensorID});
        pss = pss[0];

        let params = req.body;

        //TODO: specify what the request will have to look like

    } catch(e){
        res.status(500);
        res.send(e);
    }
})

    
module.exports = router;