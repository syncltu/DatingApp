import { User } from "./user";

export class UserParams{
    gender:string;
    minAge=18;
    maxAge=99;
    pageNumber=1;
    pageSize=5;

    orderBy='lastActive';

    constructor(user:User){
        this.gender=user.gender;
        if(user.gender==='female'){
            this.gender='male'
        }
        else{
            this.gender='female'
        }
    }
}