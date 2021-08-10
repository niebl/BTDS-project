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

    //number of inhabitants inferred
    inhabitants_inferred:{
        type: Number,
        default: 0,
    },
    //number of inhabitants naively inferred, no corrections made without RSS data.
    inhabitants_naive:{
        type: Number,
        default: 0,
    },
    //number of inhabitants as observed by the RSS
    inhabitants_observed:{
        type: Number,
        default: 0,
    },

    lastEvent:{
        type: String,
        default: Date.now()
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