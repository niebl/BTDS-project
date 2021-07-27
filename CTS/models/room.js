const mongoose = require('mongoose');

const RoomSchema = new mongoose.Schema({
    roomNumber: {
        type: String,
        index: true,
        required: true,
        unique: true
    },
    description:{
        type: String
    },
    inhabitants_inferred:{
        type: Number,
        required: true,
        default: 0,
    },
    inhabitants_observed:{
        type: Number,
        required: true,
        default: 0,
    },

    //The part that governs the RSS; reference to the sensor. 
    //  coordinates will be on RSS Schema
    aoi:{
        //lets start with support for one RSS only, to not overcomplicate things
        type: mongoose.Schema.Types.ObjectId, 
        ref: 'Aoi',
        default: null
    },

    //The part that governs the PSSs; references to the sensors
    passages:[{
        passage: {
            type: mongoose.Schema.Types.ObjectId,
            ref: "Passage"
        }
    }]
})

module.exports = mongoose.model('Room', RoomSchema)